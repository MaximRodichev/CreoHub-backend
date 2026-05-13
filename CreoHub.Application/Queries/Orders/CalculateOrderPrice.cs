using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

/// <param name="UserId">null = анонимный (скидки не применяются).</param>
public record CalculateOrderPriceQuery(List<CheckoutItemDTO> Items, Guid? UserId = null)
    : IRequest<BaseResponse<PriceBreakdownDTO>>;

public class CalculateOrderPriceHandler
    : IRequestHandler<CalculateOrderPriceQuery, BaseResponse<PriceBreakdownDTO>>
{
    private readonly IProductRepository      _productRepository;
    private readonly IAccountRepository      _accountRepository;
    private readonly IContentFileRepository  _contentFileRepository;
    private readonly IContentAccessRepository _accessRepository;

    public CalculateOrderPriceHandler(
        IProductRepository       productRepository,
        IAccountRepository       accountRepository,
        IContentFileRepository   contentFileRepository,
        IContentAccessRepository accessRepository)
    {
        _productRepository     = productRepository;
        _accountRepository     = accountRepository;
        _contentFileRepository = contentFileRepository;
        _accessRepository      = accessRepository;
    }

    public async Task<BaseResponse<PriceBreakdownDTO>> Handle(
        CalculateOrderPriceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("Items list cannot be empty.");

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products   = await _productRepository.GetProductsByIds(productIds);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("Some products were not found.");

            var productMap = products.ToDictionary(p => p.Id);

            // ── Загружаем дочерние продукты для бандлов ──────────────
            var bundleChildIds = products
                .Where(p => p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(b => b.ProductId))
                .Distinct().Except(productMap.Keys).ToList();

            if (bundleChildIds.Count > 0)
            {
                var childProducts = await _productRepository.GetProductsByIds(bundleChildIds);
                foreach (var cp in childProducts)
                    productMap.TryAdd(cp.Id, cp);
            }

            // ── Файлы дочерних продуктов бандлов + owned файлы ───────
            var allBundleChildIds = products
                .Where(p => p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(b => b.ProductId))
                .Distinct().ToList();

            var allChildFiles = allBundleChildIds.Count > 0
                ? await _contentFileRepository.GetByProductIdsAsync(allBundleChildIds)
                : new List<Domain.Entities.ContentFile>();

            var filesByChild = allChildFiles
                .GroupBy(f => f.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            HashSet<Guid> ownedFileIds = new();
            if (request.UserId.HasValue && allBundleChildIds.Count > 0)
            {
                var accesses = await _accessRepository.GetByUserIdAsync(request.UserId.Value);
                ownedFileIds = accesses.Select(a => a.ContentFileId).ToHashSet();
            }

            var lines = new List<PriceLineDTO>();

            foreach (var item in request.Items)
            {
                var product  = productMap[item.ProductId];
                var allFiles = product.ContentFiles.ToList();

                decimal price;
                bool    isPartial;

                if (item.FileIds.Count == 0)
                {
                    if (product.ProductType == ProductType.Bundle && allChildFiles.Count > 0 && ownedFileIds.Count > 0)
                    {
                        // Частичное владение — пересчитываем цену через BundleCalculator
                        var childParams = new List<(decimal BasePrice, int TotalWeight, int OwnedWeight)>();
                        foreach (var bundleItem in product.BundleItems)
                        {
                            if (!productMap.TryGetValue(bundleItem.ProductId, out var childProduct)) continue;
                            var childFileList = filesByChild.GetValueOrDefault(bundleItem.ProductId, new List<Domain.Entities.ContentFile>());
                            var totalWeight   = childFileList.Sum(f => f.PriceWeight);
                            var ownedWeight   = childFileList.Where(f => ownedFileIds.Contains(f.Id)).Sum(f => f.PriceWeight);
                            childParams.Add((childProduct.GetCurrentPrice(), totalWeight, ownedWeight));
                        }

                        if (childParams.Count > 0 && childParams.Any(c => c.OwnedWeight > 0))
                        {
                            var adj = BundleCalculator.Calculate(product.GetCurrentPrice(), childParams);
                            price     = adj.ExceedsThreshold ? product.GetCurrentPrice() : adj.FinalPrice;
                            isPartial = childParams.Any(c => c.OwnedWeight > 0 && c.OwnedWeight < c.TotalWeight);
                        }
                        else
                        {
                            price     = product.GetCurrentPrice();
                            isPartial = false;
                        }
                    }
                    else
                    {
                        price     = product.GetCurrentPrice();
                        isPartial = false;
                    }
                }
                else
                {
                    var selected = allFiles
                        .Where(cf => item.FileIds.Contains(cf.Id))
                        .ToList();

                    if (selected.Count != item.FileIds.Count)
                        throw new InvalidOperationException(
                            $"Some content files for product {item.ProductId} were not found.");

                    price     = product.CalculatePrice(selected);
                    isPartial = true;
                }

                lines.Add(new PriceLineDTO
                {
                    ProductId          = product.Id,
                    ProductName        = product.Name,
                    SelectedFilesCount = item.FileIds.Count == 0 ? allFiles.Count : item.FileIds.Count,
                    TotalFilesCount    = allFiles.Count,
                    IsPartialPurchase  = isPartial,
                    Price              = price,
                    OriginalPrice      = product.GetCurrentPrice(),
                });
            }

            var subtotal = lines.Sum(l => l.Price);

            // ── Скидки (F1 + F2) ─────────────────────────────────────
            decimal lifetimeDisc    = 0m;
            // F2: скидка по количеству товаров (3+→3%, 5+→6%, 8+→9%)
            decimal cartVolumeDisc  = DiscountCalculator.GetCartCountDiscount(request.Items.Count);

            if (request.UserId.HasValue)
            {
                var user = await _accountRepository.GetFullInfoByIdAsync(request.UserId.Value);
                if (user != null)
                    lifetimeDisc = user.GetLifetimeDiscount();
            }

            var totalDisc    = DiscountCalculator.GetTotalDiscount(lifetimeDisc, cartVolumeDisc);
            var discountAmt  = subtotal * totalDisc;
            var total        = DiscountCalculator.ApplyDiscount(subtotal, totalDisc);

            return BaseResponse<PriceBreakdownDTO>.Success(new PriceBreakdownDTO
            {
                Subtotal        = subtotal,
                Total           = total,
                DiscountPercent = totalDisc * 100m,
                DiscountAmount  = discountAmt,
                DiscountBreakdown = new DiscountBreakdownDTO
                {
                    LifetimeDiscountPercent    = lifetimeDisc    * 100m,
                    CartVolumeDiscountPercent  = cartVolumeDisc  * 100m,
                },
                Lines = lines,
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<PriceBreakdownDTO>.Fail(ex.Message);
        }
    }
}

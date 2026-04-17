using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

public record CalculateOrderPriceQuery(List<CheckoutItemDTO> Items)
    : IRequest<BaseResponse<PriceBreakdownDTO>>;

public class CalculateOrderPriceHandler
    : IRequestHandler<CalculateOrderPriceQuery, BaseResponse<PriceBreakdownDTO>>
{
    private readonly IProductRepository _productRepository;

    public CalculateOrderPriceHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<BaseResponse<PriceBreakdownDTO>> Handle(
        CalculateOrderPriceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("Items list cannot be empty.");

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _productRepository.GetProductsByIds(productIds);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("Some products were not found.");

            var productMap = products.ToDictionary(p => p.Id);
            var lines = new List<PriceLineDTO>();

            foreach (var item in request.Items)
            {
                var product = productMap[item.ProductId];
                var allFiles = product.ContentFiles.ToList();

                decimal price;
                bool isPartial;

                if (item.FileIds.Count == 0)
                {
                    // Полная покупка
                    price = product.GetCurrentPrice();
                    isPartial = false;
                }
                else
                {
                    var selected = allFiles
                        .Where(cf => item.FileIds.Contains(cf.Id))
                        .ToList();

                    if (selected.Count != item.FileIds.Count)
                        throw new InvalidOperationException(
                            $"Some content files for product {item.ProductId} were not found.");

                    price = product.CalculatePrice(selected);
                    isPartial = true;
                }

                lines.Add(new PriceLineDTO
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SelectedFilesCount = item.FileIds.Count == 0 ? allFiles.Count : item.FileIds.Count,
                    TotalFilesCount = allFiles.Count,
                    IsPartialPurchase = isPartial,
                    Price = price
                });
            }

            return BaseResponse<PriceBreakdownDTO>.Success(new PriceBreakdownDTO
            {
                Total = lines.Sum(l => l.Price),
                Lines = lines
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<PriceBreakdownDTO>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Pricing;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Services;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.Extensions.Options;

namespace CreoHub.Application.Commands.OrderCommands;

public record CreateCheckoutCommand(Guid UserId, List<CheckoutItemDTO> Items)
    : IRequest<BaseResponse<CheckoutResultDTO>>;

public class CreateCheckoutHandler : IRequestHandler<CreateCheckoutCommand, BaseResponse<CheckoutResultDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IPaymentGatewayService _paymentService;
    private readonly IAccountRepository _accountRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IContentAccessRepository _accessRepository;
    private readonly PricingConfig            _pricing;

    public CreateCheckoutHandler(
        IUnitOfWork unitOfWork,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserTransactionRepository transactionRepository,
        IPaymentGatewayService paymentService,
        IAccountRepository accountRepository,
        IContentFileRepository contentFileRepository,
        IContentAccessRepository accessRepository,
        IOptions<PricingConfig> pricing)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _transactionRepository = transactionRepository;
        _paymentService = paymentService;
        _accountRepository = accountRepository;
        _contentFileRepository = contentFileRepository;
        _accessRepository = accessRepository;
        _pricing = pricing.Value;
    }

    public async Task<BaseResponse<CheckoutResultDTO>> Handle(
        CreateCheckoutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("Items list cannot be empty.");

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

            // Грузим продукты с ценами и файлами (GetProductsByIds уже включает Prices + ContentFiles)
            var products = await _productRepository.GetProductsByIds(productIds);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("Some products were not found.");

            var productMap = products.ToDictionary(p => p.Id);

            // ── Проверка коллизии: товар И бандл с этим товаром одновременно ──
            var allBundleChildIdSet = products
                .Where(p => p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(b => b.ProductId))
                .ToHashSet();

            var conflictIds = productIds
                .Where(id => productMap.TryGetValue(id, out var p)
                             && p.ProductType != ProductType.Bundle
                             && allBundleChildIdSet.Contains(id))
                .ToList();

            if (conflictIds.Any())
            {
                var names = string.Join(", ", conflictIds.Select(id => $"«{productMap[id].Name}»"));
                return BaseResponse<CheckoutResultDTO>.Fail(
                    $"Нельзя купить одновременно товар и набор, в который он входит: {names}. " +
                    "Удалите отдельный товар из корзины — он уже включён в набор.");
            }

            // ── Загружаем уже купленные файлы пользователя ───────────
            var ownedAccesses = await _accessRepository.GetByUserIdAsync(request.UserId);
            var ownedFileIds  = ownedAccesses.Select(a => a.ContentFileId).ToHashSet();

            // ── Загружаем дочерние продукты бандлов ─────────────────
            var bundleChildIds = products
                .Where(p => p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(b => b.ProductId))
                .Distinct()
                .Except(productMap.Keys)
                .ToList();

            if (bundleChildIds.Count > 0)
            {
                var childProducts = await _productRepository.GetProductsByIds(bundleChildIds);
                foreach (var cp in childProducts)
                    productMap.TryAdd(cp.Id, cp);
            }

            // ── Формируем items для Order.Open() с выбранными файлами
            var orderItems = new List<(Product product, List<ContentFile> selectedFiles)>();

            foreach (var item in request.Items)
            {
                var product = productMap[item.ProductId];

                List<ContentFile> selectedFiles;
                if (item.FileIds.Count == 0)
                {
                    if (product.ProductType == ProductType.Bundle)
                    {
                        // Бандл: проверяем, что не все дочерние файлы уже куплены
                        var childProductIds = product.BundleItems.Select(b => b.ProductId).ToList();
                        var allChildFiles   = await _contentFileRepository.GetByProductIdsAsync(childProductIds);
                        if (allChildFiles.Count > 0 && allChildFiles.All(f => ownedFileIds.Contains(f.Id)))
                            return BaseResponse<CheckoutResultDTO>.Fail(
                                $"Вы уже приобрели все файлы из набора «{product.Name}».");
                    }
                    else
                    {
                        // Обычный продукт: проверяем, что не все файлы уже куплены
                        var allFiles = product.ContentFiles.ToList();
                        var nonOwned = allFiles.Where(cf => !ownedFileIds.Contains(cf.Id)).ToList();
                        if (allFiles.Count > 0 && nonOwned.Count == 0)
                            return BaseResponse<CheckoutResultDTO>.Fail(
                                $"Все файлы продукта «{product.Name}» уже куплены вами.");
                    }
                    selectedFiles = new List<ContentFile>();
                }
                else
                {
                    // Частичная покупка: проверяем, что выбранные файлы не куплены
                    var alreadyOwned = item.FileIds.Where(id => ownedFileIds.Contains(id)).ToList();
                    if (alreadyOwned.Any())
                        return BaseResponse<CheckoutResultDTO>.Fail(
                            $"Некоторые файлы из «{product.Name}» уже куплены вами.");

                    selectedFiles = product.ContentFiles
                        .Where(cf => item.FileIds.Contains(cf.Id))
                        .ToList();

                    if (selectedFiles.Count != item.FileIds.Count)
                        throw new InvalidOperationException(
                            $"Some content files for product {item.ProductId} were not found.");
                }

                orderItems.Add((product, selectedFiles));
            }

            var order = Order.Open(
                description:  string.Empty,
                items:        orderItems,
                customerId:   request.UserId,
                computeAlpha: _pricing.ComputeAlpha);

            // ── Рассчитываем и сохраняем снимок скидок ──────────────
            var user         = await _accountRepository.GetFullInfoByIdAsync(request.UserId);
            var lifetimeDisc = user?.GetLifetimeDiscount() ?? 0m;
            var cartDisc     = DiscountCalculator.GetCartVolumeDiscount(order.Subtotal);
            order.ApplyDiscounts(lifetimeDisc, cartDisc);

            await _orderRepository.AddAsync(order);

            // Сохраняем Order первым — чтобы его Id попал в БД до создания транзакции
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Создаём инвойс в OxaPay на итоговую сумму (после скидок)
            var invoice = await _paymentService.CreateInvoiceAsync(order.Price, order.Id.ToString());

            // Создаём транзакцию и привязываем к заказу
            var transaction = UserTransaction.CreatePurchase(order.Price, request.UserId, invoice.TrackId, order);
            await _transactionRepository.AddAsync(transaction);
            order.AttachTransaction(transaction);

            // Сохраняем транзакцию и обновлённый Order (TransactionId)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<CheckoutResultDTO>.Success(new CheckoutResultDTO
            {
                OrderId = order.Id,
                PaymentUrl = invoice.PaymentUrl,
                ExpiresAt = invoice.ExpiredAt
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<CheckoutResultDTO>.Fail(ex.Message);
        }
    }
}

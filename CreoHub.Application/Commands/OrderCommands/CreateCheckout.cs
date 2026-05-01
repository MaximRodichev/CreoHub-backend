using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Services;
using CreoHub.Domain.Types;
using MediatR;

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

    public CreateCheckoutHandler(
        IUnitOfWork unitOfWork,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserTransactionRepository transactionRepository,
        IPaymentGatewayService paymentService,
        IAccountRepository accountRepository)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _transactionRepository = transactionRepository;
        _paymentService = paymentService;
        _accountRepository = accountRepository;
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

            // Формируем items для Order.Open() с выбранными файлами
            var orderItems = new List<(Product product, List<ContentFile> selectedFiles)>();

            foreach (var item in request.Items)
            {
                var product = productMap[item.ProductId];

                List<ContentFile> selectedFiles;
                if (item.FileIds.Count == 0)
                {
                    // Полная покупка
                    selectedFiles = new List<ContentFile>();
                }
                else
                {
                    selectedFiles = product.ContentFiles
                        .Where(cf => item.FileIds.Contains(cf.Id))
                        .ToList();

                    if (selectedFiles.Count != item.FileIds.Count)
                        throw new InvalidOperationException(
                            $"Some content files for product {item.ProductId} were not found.");
                }

                orderItems.Add((product, selectedFiles));
            }

            var order = Order.Open(description: string.Empty, items: orderItems, customerId: request.UserId);

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

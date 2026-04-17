using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.OrderCommands;

public record CompletePaymentCommand(string TrackId, string TxHash, string SenderAddress)
    : IRequest<BaseResponse<bool>>;

public class CompletePaymentHandler : IRequestHandler<CompletePaymentCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IContentAccessRepository _accessRepository;
    private readonly IProductRepository _productRepository;
    private readonly IShopTransactionRepository _shopTransactionRepository;
    private readonly IShopBalanceRepository _shopBalanceRepository;

    public CompletePaymentHandler(
        IUnitOfWork unitOfWork,
        IUserTransactionRepository transactionRepository,
        IOrderRepository orderRepository,
        IContentFileRepository contentFileRepository,
        IContentAccessRepository accessRepository,
        IProductRepository productRepository,
        IShopTransactionRepository shopTransactionRepository,
        IShopBalanceRepository shopBalanceRepository)
    {
        _unitOfWork = unitOfWork;
        _transactionRepository = transactionRepository;
        _orderRepository = orderRepository;
        _contentFileRepository = contentFileRepository;
        _accessRepository = accessRepository;
        _productRepository = productRepository;
        _shopTransactionRepository = shopTransactionRepository;
        _shopBalanceRepository = shopBalanceRepository;
    }

    public async Task<BaseResponse<bool>> Handle(
        CompletePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _transactionRepository.GetByTrackIdAsync(request.TrackId)
                ?? throw new InvalidOperationException(
                    $"Transaction with trackId '{request.TrackId}' not found.");

            // Грузим заказ с Items и их выбранными файлами
            var order = await _orderRepository.GetByTransactionIdWithItemsAsync(transaction.Id)
                ?? throw new InvalidOperationException(
                    $"Order for transaction '{request.TrackId}' not found.");

            // Помечаем транзакцию успешной и закрываем заказ
            transaction.Success(request.SenderAddress, request.TxHash);
            order.Complete();

            // ── Зачисляем выручку на баланс магазина ─────────────────
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products   = await _productRepository.GetProductsByIds(productIds);
            var productMap = products.ToDictionary(p => p.Id);

            var shopRevenue = order.Items
                .Where(i => productMap.ContainsKey(i.ProductId))
                .GroupBy(i => productMap[i.ProductId].OwnerId)
                .Select(g => new { ShopId = g.Key, Amount = g.Sum(i => i.PriceAtPurchase) });

            foreach (var revenue in shopRevenue)
            {
                var shopTx = ShopTransaction.CreateShopSale(
                    amount:  revenue.Amount,
                    shopId:  revenue.ShopId,
                    trackId: $"sale-{order.Id}-{revenue.ShopId}",
                    order:   order);
                shopTx.SuccessInternal();
                await _shopTransactionRepository.AddAsync(shopTx);

                var shopBalance = await _shopBalanceRepository.GetByShopIdAsync(revenue.ShopId);
                if (shopBalance is null)
                {
                    shopBalance = new ShopBalance(revenue.ShopId);
                    shopBalance.AddFunds(shopTx.NetAmount);
                    await _shopBalanceRepository.AddAsync(shopBalance);
                }
                else
                {
                    shopBalance.AddFunds(shopTx.NetAmount);
                    _shopBalanceRepository.Update(shopBalance);
                }
            }

            // Выдаём доступ к файлам по каждому OrderItem
            foreach (var item in order.Items)
            {
                if (item.Files.Any())
                {
                    // Частичная покупка — только выбранные файлы
                    foreach (var file in item.Files)
                    {
                        await _accessRepository.AddAsync(
                            new ContentAccess(order.CustomerId, file.ContentFileId, order.Id));
                    }
                }
                else
                {
                    // Полная покупка — все файлы продукта
                    var allFiles = await _contentFileRepository.GetByProductIdAsync(item.ProductId);
                    foreach (var file in allFiles)
                    {
                        await _accessRepository.AddAsync(
                            new ContentAccess(order.CustomerId, file.Id, order.Id));
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

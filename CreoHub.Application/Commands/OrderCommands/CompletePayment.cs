using Creohub.Domain.Entities;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
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
    private readonly IAccountRepository _accountRepository;
    private readonly ISubscriptionPromoCodeRepository _promoCodeRepository;
    private readonly ICartRepository _cartRepository;

    // Пороги LifetimeSpent → промо-коды AutoSlot
    private static readonly (decimal Threshold, int Days, string Tag)[] Milestones =
    [
        (500m,  90,  "lifetime_500"),
        (1000m, 180, "lifetime_1000"),
        (2500m, 365, "lifetime_2500"),
    ];

    public CompletePaymentHandler(
        IUnitOfWork unitOfWork,
        IUserTransactionRepository transactionRepository,
        IOrderRepository orderRepository,
        IContentFileRepository contentFileRepository,
        IContentAccessRepository accessRepository,
        IProductRepository productRepository,
        IShopTransactionRepository shopTransactionRepository,
        IShopBalanceRepository shopBalanceRepository,
        IAccountRepository accountRepository,
        ISubscriptionPromoCodeRepository promoCodeRepository,
        ICartRepository cartRepository)
    {
        _unitOfWork = unitOfWork;
        _transactionRepository = transactionRepository;
        _orderRepository = orderRepository;
        _contentFileRepository = contentFileRepository;
        _accessRepository = accessRepository;
        _productRepository = productRepository;
        _shopTransactionRepository = shopTransactionRepository;
        _shopBalanceRepository = shopBalanceRepository;
        _accountRepository = accountRepository;
        _promoCodeRepository = promoCodeRepository;
        _cartRepository = cartRepository;
    }

    public async Task<BaseResponse<bool>> Handle(
        CompletePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _transactionRepository.GetByTrackIdAsync(request.TrackId)
                ?? throw new InvalidOperationException(
                    $"Transaction with trackId '{request.TrackId}' not found.");

            var order = await _orderRepository.GetByTransactionIdWithItemsAsync(transaction.Id)
                ?? throw new InvalidOperationException(
                    $"Order for transaction '{request.TrackId}' not found.");

            transaction.Success(request.SenderAddress, request.TxHash);
            order.Complete();

            // ── Выручка магазинов ─────────────────────────────────────────
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

            // ── Доступ к файлам ───────────────────────────────────────────
            foreach (var item in order.Items)
            {
                if (item.Files.Any())
                {
                    foreach (var file in item.Files)
                        await _accessRepository.AddAsync(
                            new ContentAccess(order.CustomerId, file.ContentFileId, order.Id));
                }
                else
                {
                    var allFiles = await _contentFileRepository.GetByProductIdAsync(item.ProductId);
                    foreach (var file in allFiles)
                        await _accessRepository.AddAsync(
                            new ContentAccess(order.CustomerId, file.Id, order.Id));
                }
            }

            // ── LifetimeSpent + milestone промо-коды ──────────────────────
            var orderTotal = order.Items.Sum(i => i.PriceAtPurchase);
            var user = await _accountRepository.GetByIdAsync(order.CustomerId);
            if (user != null)
            {
                user.AddSpend(orderTotal);
                await CheckMilestonesAsync(user);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── Очистка корзины: удаляем только купленные позиции ─────────
            var purchasedProductIds = order.Items.Select(i => i.ProductId).Distinct();
            await _cartRepository.RemoveCartItemsByProductIdsAsync(order.CustomerId, purchasedProductIds);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }

    private async Task CheckMilestonesAsync(Domain.Entities.User user)
    {
        foreach (var (threshold, days, tag) in Milestones)
        {
            if (user.LifetimeSpent < threshold) continue;

            var alreadyIssued = await _promoCodeRepository.WasMilestoneIssuedAsync(user.Id, tag);
            if (alreadyIssued) continue;

            var promo = SubscriptionPromoCode.CreateForMilestone(
                issuedToUserId: user.Id,
                product:        SubscriptionProductType.AutoSlot,
                days:           days,
                milestoneTag:   tag);

            await _promoCodeRepository.AddAsync(promo);

            // TODO: уведомить пользователя (email/Telegram) с кодом promo.Code
            // await _notificationService.SendPromoCodeAsync(user, promo.Code, days);
        }
    }
}

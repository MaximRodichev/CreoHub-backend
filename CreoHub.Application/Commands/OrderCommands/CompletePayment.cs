using Creohub.Domain.Entities;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    private readonly IEventTracker _events;
    private readonly INotificationService _notifications;

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
        ICartRepository cartRepository,
        IEventTracker events,
        INotificationService notifications)
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
        _events = events;
        _notifications = notifications;
    }

    public async Task<BaseResponse<bool>> Handle(
        CompletePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _transactionRepository.GetByTrackIdAsync(request.TrackId)
                ?? throw new InvalidOperationException(
                    $"Transaction with trackId '{request.TrackId}' not found.");

            // Idempotency: webhook может прийти дважды — уже обработанный trackId игнорируем
            if (transaction.TransactionStatus == TransactionStatus.Completed)
                return BaseResponse<bool>.Success(true);

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
            var purchasedProductIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            await _cartRepository.RemoveCartItemsByProductIdsAsync(order.CustomerId, purchasedProductIds);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── Analytics events ──────────────────────────────────────────
            _events.Track(EventTypes.CheckoutCompleted,
                userId: order.CustomerId);

            foreach (var productId in purchasedProductIds)
                _events.Track(EventTypes.ProductPurchased,
                    productId: productId,
                    userId:    order.CustomerId);

            // ── Purchase notifications to sellers (fire-and-forget) ────────
            var notifTargets = await BuildSellerNotificationsAsync(productMap, order, cancellationToken);
            _ = SendSellerNotificationsAsync(notifTargets);

            return BaseResponse<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Гонка webhook'ов: другой запрос уже завершил этот заказ — идемпотентный успех.
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception)
        {
            return BaseResponse<bool>.Fail("Не удалось обработать платёж.");
        }
    }

    private async Task<List<(Guid UserId, long? TelegramId, string? Email, string Message)>> BuildSellerNotificationsAsync(
        Dictionary<int, Domain.Entities.Product> productMap,
        Order order,
        CancellationToken ct)
    {
        var result = new List<(Guid, long?, string?, string)>();
        try
        {
            var shopIds = productMap.Values.Select(p => p.OwnerId).Distinct();
            foreach (var shopId in shopIds)
            {
                var seller = await _accountRepository.GetUserByShopIdAsync(shopId, ct);
                var ns = seller?.NotificationSettings;
                if (seller is null || ns is null || !ns.NotifyOnPurchase) continue;

                var productNames = productMap.Values
                    .Where(p => p.OwnerId == shopId)
                    .Select(p => p.Name);

                var amount = order.Items
                    .Where(i => productMap.ContainsKey(i.ProductId) && productMap[i.ProductId].OwnerId == shopId)
                    .Sum(i => i.PriceAtPurchase);

                result.Add((seller.Id,
                    ns.TelegramEnabled ? seller.TelegramId   : null,
                    ns.EmailEnabled    ? seller.EmailAddress : null,
                    $"Новая продажа: {string.Join(", ", productNames)}. Сумма: {amount:F2} USDT"));
            }
        }
        catch { /* не критично */ }
        return result;
    }

    private async Task SendSellerNotificationsAsync(
        List<(Guid UserId, long? TelegramId, string? Email, string Message)> targets)
    {
        try
        {
            foreach (var (userId, tg, email, msg) in targets)
                await _notifications.NotifyAsync(userId, Domain.Types.NotificationType.Purchase,
                    msg, actionUrl: null, tg, email, CancellationToken.None);
        }
        catch { }
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

            // Notify user about their promo code (fire-and-forget)
            _ = _notifications.SendAsync(
                user.TelegramId, user.EmailAddress,
                $"Вы получили промо-код на {days} дней AutoSlot: {promo.Code}",
                default);
        }
    }
}

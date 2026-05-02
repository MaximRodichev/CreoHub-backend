using Creohub.Domain.Entities;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Creohub.AutoSlot.Services;

public class SubscriptionService(AppDbContext db)
{
    public async Task<bool> IsActiveAsync(Guid userId, SubscriptionProductType product) =>
        await db.Subscriptions.AnyAsync(s =>
            s.UserId == userId &&
            s.ProductType == product &&
            (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow));

    public async Task<DateTime?> GetLatestExpiresAtAsync(Guid userId, SubscriptionProductType product) =>
        await db.Subscriptions
            .Where(s =>
                s.UserId == userId &&
                s.ProductType == product &&
                s.ExpiresAt != null &&
                s.ExpiresAt > DateTime.UtcNow)
            .MaxAsync(s => (DateTime?)s.ExpiresAt);

    public async Task<List<Subscription>> GetHistoryAsync(Guid userId, SubscriptionProductType product) =>
        await db.Subscriptions
            .Where(s => s.UserId == userId && s.ProductType == product)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task GrantAsync(Guid userId, SubscriptionProductType product,
        int days, bool isLifetime = false, Guid? promoCodeId = null)
    {
        var currentExpiresAt = await GetLatestExpiresAtAsync(userId, product);

        var plan = isLifetime ? SubscriptionPlanType.Lifetime : SubscriptionPlanType.Monthly;
        var sub = Subscription.Create(
            userId: userId,
            product: product,
            plan: plan,
            days: isLifetime ? 0 : days,
            currentExpiresAt: currentExpiresAt,
            promoCodeId: promoCodeId);

        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Покупка подписки с баланса пользователя.
    /// Возвращает (true, "") при успехе или (false, errorMessage) при ошибке.
    /// </summary>
    public async Task<(bool success, string error)> PurchaseAsync(
        Guid userId, int days, decimal price, SubscriptionProductType product)
    {
        var balance = await db.UserBalances.FirstOrDefaultAsync(b => b.UserId == userId);
        if (balance == null || balance.AvailableAmount < price)
            return (false, "Недостаточно средств на балансе");

        var planType = days switch
        {
            <= 7   => SubscriptionPlanType.Monthly,
            <= 30  => SubscriptionPlanType.Monthly,
            <= 180 => SubscriptionPlanType.Quarterly,
            _      => SubscriptionPlanType.Yearly,
        };

        // Тратим с баланса
        balance.Spend(price);
        db.UserBalances.Update(balance);

        // Создаём транзакцию
        var trackId = $"as-sub-{Guid.NewGuid():N}";
        var tx = UserTransaction.CreateSubscriptionPurchase(price, userId, trackId);
        tx.SuccessInternal();
        await db.UserTransactions.AddAsync(tx);

        // Создаём подписку, стекуя поверх текущей если она есть
        var currentExpiresAt = await GetLatestExpiresAtAsync(userId, product);
        var sub = Subscription.Create(
            userId:           userId,
            product:          product,
            plan:             planType,
            days:             days,
            currentExpiresAt: currentExpiresAt,
            transactionId:    tx.Id);

        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        return (true, string.Empty);
    }

    public async Task<bool> RedeemPromoCodeAsync(Guid userId, string code)
    {
        var promo = await db.SubscriptionPromoCodes
            .FirstOrDefaultAsync(p => p.Code == code.ToUpper() && !p.IsUsed);

        if (promo == null) return false;
        if (promo.ExpiresAt.HasValue && promo.ExpiresAt < DateTime.UtcNow) return false;

        await GrantAsync(userId, promo.ProductType, promo.Days, promo.IsLifetime, promo.Id);

        promo.Use(userId);
        await db.SaveChangesAsync();
        return true;
    }
}

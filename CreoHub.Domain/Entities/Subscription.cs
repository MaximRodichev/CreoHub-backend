using CreoHub.Domain.Types;

namespace Creohub.Domain.Entities;

public class Subscription
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public Guid UserId { get; private init; }
    public SubscriptionProductType ProductType { get; private init; }
    public SubscriptionPlanType PlanType { get; private init; }
    public int Days { get; private init; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; private init; }  // null = Lifetime
    public Guid? TransactionId { get; private init; }
    public Guid? PromoCodeId { get; private init; }

    public static Subscription Create(
        Guid userId,
        SubscriptionProductType product,
        SubscriptionPlanType plan,
        int days,
        DateTime? currentExpiresAt,
        Guid? transactionId = null,
        Guid? promoCodeId = null)
    {
        var baseDate = currentExpiresAt.HasValue && currentExpiresAt > DateTime.UtcNow
            ? currentExpiresAt.Value
            : DateTime.UtcNow;

        return new Subscription
        {
            UserId        = userId,
            ProductType   = product,
            PlanType      = plan,
            Days          = days,
            ExpiresAt     = plan == SubscriptionPlanType.Lifetime ? null : baseDate.AddDays(days),
            TransactionId = transactionId,
            PromoCodeId   = promoCodeId
        };
    }
}

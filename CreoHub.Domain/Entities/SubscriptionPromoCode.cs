using CreoHub.Domain.Types;

namespace Creohub.Domain.Entities;

public class SubscriptionPromoCode
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Code { get; private init; }
    public SubscriptionProductType ProductType { get; private init; }
    public int Days { get; private init; }
    public bool IsLifetime { get; private init; }
    public bool IsUsed { get; private set; } = false;
    public Guid? UsedByUserId { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; private init; }

    // Кому создан (не обязательно кто активирует — можно подарить)
    public Guid? IssuedToUserId { get; private init; }
    // Дедупликатор для автотриггеров: "lifetime_500", "lifetime_1000" и т.д.
    public string? MilestoneTag { get; private init; }

    // Ручное создание (adminController, акции)
    public static SubscriptionPromoCode Create(string code, SubscriptionProductType product,
        int days, bool isLifetime = false, DateTime? expiresAt = null)
        => new()
        {
            Code = code.ToUpper(),
            ProductType = product,
            Days = days,
            IsLifetime = isLifetime,
            ExpiresAt = expiresAt
        };

    // Автотриггер (LifetimeSpent milestone) — код генерируется автоматически
    public static SubscriptionPromoCode CreateForMilestone(
        Guid issuedToUserId,
        SubscriptionProductType product,
        int days,
        string milestoneTag,
        DateTime? expiresAt = null)
        => new()
        {
            Code = "AUTOSLOT-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            ProductType = product,
            Days = days,
            IssuedToUserId = issuedToUserId,
            MilestoneTag = milestoneTag,
            ExpiresAt = expiresAt
        };

    public void Use(Guid userId)
    {
        IsUsed = true;
        UsedByUserId = userId;
        UsedAt = DateTime.UtcNow;
    }
}

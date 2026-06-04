namespace CreoHub.Domain.Entities;

/// <summary>
/// Подписка пользователя на магазин. Композитный ключ (UserId, ShopId)
/// гарантирует уникальность — нельзя подписаться дважды.
/// Используется для уведомлений о новых товарах магазина.
/// </summary>
public class ShopFollow
{
    public Guid     UserId    { get; private init; }
    public Guid     ShopId    { get; private init; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

    private ShopFollow() {}

    public static ShopFollow Create(Guid userId, Guid shopId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (shopId == Guid.Empty)
            throw new ArgumentException("ShopId is required.", nameof(shopId));

        return new ShopFollow
        {
            UserId    = userId,
            ShopId    = shopId,
            CreatedAt = DateTime.UtcNow,
        };
    }
}

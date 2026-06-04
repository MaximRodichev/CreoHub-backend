using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;

namespace CreoHub.Application.Commands.ShopFollows;

/// <summary>
/// Общий помощник для рассылки уведомления подписчикам магазина о новом товаре.
/// Используется двумя триггерами: одобрение модератором и повторная публикация
/// продавцом (Hidden → Active). Fire-and-forget — ошибки не влияют на основной поток.
/// </summary>
internal static class ShopFollowerNotifier
{
    public static async Task NotifyNewProductAsync(
        IShopFollowRepository follows,
        IShopRepository       shops,
        INotificationService  notifications,
        Guid                  shopId,
        string                productName,
        string                productSlug)
    {
        try
        {
            var followers = await follows.GetFollowersForNotificationAsync(shopId);
            if (followers.Count == 0) return;

            var shop = await shops.GetShopByIdAsync(shopId);
            if (shop is null) return;

            var msg = $"В магазине «{shop.Name}» появился новый товар: {productName}";
            var url = $"/store/product/{productSlug}";

            foreach (var f in followers)
            {
                var tg = f.TelegramEnabled ? f.TelegramId   : null;
                var em = f.EmailEnabled    ? f.EmailAddress : null;
                await notifications.NotifyAsync(f.UserId, NotificationType.NewProduct,
                    msg, actionUrl: url, tg, em, CancellationToken.None);
            }
        }
        catch { /* notifications must never affect the main flow */ }
    }
}

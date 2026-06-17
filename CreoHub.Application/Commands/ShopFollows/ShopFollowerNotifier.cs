using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Application.Commands.ShopFollows;

/// <summary>
/// Рассылка уведомления подписчикам магазина о новом товаре.
/// Используется двумя триггерами: одобрение модератором и повторная публикация
/// продавцом (Hidden → Active).
///
/// Запускается как fire-and-forget, поэтому ОБЯЗАН работать в собственном DI-scope:
/// scoped DbContext исходного запроса к моменту рассылки уже может быть закрыт
/// (или использоваться другим потоком — "second operation on context").
/// </summary>
internal static class ShopFollowerNotifier
{
    /// <summary>
    /// Создаёт отдельный scope (свой DbContext) и рассылает уведомления.
    /// Все исключения поглощаются — фоновая рассылка не влияет на основной поток.
    /// </summary>
    public static async Task NotifyNewProductInScopeAsync(
        IServiceScopeFactory scopeFactory,
        Guid                 shopId,
        string               productName,
        string               productSlug)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var follows       = scope.ServiceProvider.GetRequiredService<IShopFollowRepository>();
            var shops         = scope.ServiceProvider.GetRequiredService<IShopRepository>();
            var notifications  = scope.ServiceProvider.GetRequiredService<INotificationService>();

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

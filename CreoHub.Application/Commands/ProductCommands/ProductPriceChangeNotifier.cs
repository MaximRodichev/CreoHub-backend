using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Application.Commands.ProductCommands;

/// <summary>
/// Рассылка in-app уведомления о смене цены товара тем, у кого он лежит в корзине.
/// Fire-and-forget → работает в собственном DI-scope (свой DbContext, как ShopFollowerNotifier).
/// </summary>
internal static class ProductPriceChangeNotifier
{
    public static async Task NotifyInScopeAsync(
        IServiceScopeFactory scopeFactory,
        int                  productId,
        string               productName,
        string               productSlug,
        bool                 wentUp)
    {
        try
        {
            using var scope   = scopeFactory.CreateScope();
            var carts         = scope.ServiceProvider.GetRequiredService<ICartRepository>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var userIds = await carts.GetUserIdsWithProductAsync(productId);
            if (userIds.Count == 0) return;

            var verb = wentUp ? "подорожал" : "подешевел";
            var msg  = $"Товар из вашей корзины «{productName}» {verb}.";
            var url  = $"/store/product/{productSlug}";

            foreach (var userId in userIds)
            {
                // In-app only: каналы TG/email намеренно не передаём.
                await notifications.NotifyAsync(userId, NotificationType.ProductPriceChanged,
                    msg, actionUrl: url, null, null, CancellationToken.None);
            }
        }
        catch { /* notifications must never affect the main flow */ }
    }
}

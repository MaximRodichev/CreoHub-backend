using CreoHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Application.Commands.ProductCommands;

/// <summary>
/// Фоновая генерация og-карточки товара в собственном DI-scope (свой DbContext).
/// Fire-and-forget — не блокирует основной поток и не влияет на него при ошибке.
/// </summary>
internal static class ProductOgGenerator
{
    public static async Task GenerateInScopeAsync(IServiceScopeFactory scopeFactory, int productId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IProductOgImageService>();
            await svc.GenerateAndStoreAsync(productId);
        }
        catch { /* og-генерация не должна влиять на основной поток */ }
    }
}

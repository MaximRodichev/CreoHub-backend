using Creohub.AutoSlot.Middleware;
using Creohub.AutoSlot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Creohub.AutoSlot;

public static class AutoSlotExtensions
{
    public static IServiceCollection AddAutoSlot(this IServiceCollection services)
    {
        services.AddScoped<PanelSessionService>();
        services.AddScoped<PanelJwtService>();
        services.AddScoped<SubscriptionService>();
        return services;
    }

    public static IApplicationBuilder UseAutoSlot(this IApplicationBuilder app)
    {
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/autoslot"),
            branch => branch.UseMiddleware<AutoSlotMiddleware>()
        );
        return app;
    }
}

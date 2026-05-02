using Creohub.AutoSlot.Services;
using CreoHub.Domain.Types;
using Microsoft.AspNetCore.Http;

namespace Creohub.AutoSlot.Middleware;

public class AutoSlotMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, PanelJwtService jwtService, SubscriptionService subscriptionService)
    {
        var path = context.Request.Path;

        // Auth роуты — полностью пропускаем
        if (path.StartsWithSegments("/autoslot/auth"))
        {
            await next(context);
            return;
        }

        // Все остальные autoslot роуты требуют валидного cookie
        var token = context.Request.Cookies["autoslot_session"];
        if (string.IsNullOrEmpty(token))
        {
            context.Response.Redirect("/autoslot/auth/start");
            return;
        }

        var userId = jwtService.ValidateAndExtractUserId(token);
        if (userId == null)
        {
            context.Response.Cookies.Delete("autoslot_session");
            context.Response.Redirect("/autoslot/auth/start");
            return;
        }

        // userId доступен всем downstream хэндлерам
        context.Items["AutoSlotUserId"] = userId.Value;

        // Subscribe, redeem и subscription-ping не требуют активной подписки
        if (path.StartsWithSegments("/autoslot/subscribe") ||
            path.StartsWithSegments("/autoslot/redeem")       ||
            path.StartsWithSegments("/autoslot/subscription-ping"))
        {
            await next(context);
            return;
        }

        // Всё остальное требует активной подписки
        var hasAccess = await subscriptionService.IsActiveAsync(userId.Value, SubscriptionProductType.AutoSlot);
        if (!hasAccess)
        {
            context.Response.Redirect("/autoslot/subscribe");
            return;
        }

        await next(context);
    }
}

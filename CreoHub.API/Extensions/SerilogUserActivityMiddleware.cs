using Serilog;

namespace CreoHub.API.DI;

public class SerilogUserActivityMiddleware
{
    private readonly RequestDelegate _next;

    public SerilogUserActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Получаем IP
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        // 2. Получаем данные из JWT (если пользователь авторизован)
        // Ищем Claim "name" или "sub" или любой другой, который ты кладешь в токен
        var userName = context.User.Identity?.IsAuthenticated == true 
            ? context.User.Identity.Name ?? context.User.FindFirst("id")?.Value 
            : "Anonymous";

        var method = context.Request.Method;
        var path = context.Request.Path;

        // Логируем красиво
        Log.Information("User: {User} | IP: {IP} | Request: {Method} {Path}", 
            userName, ipAddress, method, path);

        await _next(context);
    }
}
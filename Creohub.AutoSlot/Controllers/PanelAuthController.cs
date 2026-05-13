using System.Security.Claims;
using Creohub.AutoSlot.Services;
using CreoHub.Domain.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Creohub.AutoSlot.Controllers;

[Route("autoslot/auth")]
[EnableCors("AllowPanel")]
public class PanelAuthController(
    PanelSessionService sessions,
    PanelJwtService jwtService,
    SubscriptionService subscriptionService,
    IConfiguration config) : Controller
{
    private readonly string _frontendUrl = config["Frontend"]!;

    [HttpGet("start")]
    public async Task<IActionResult> Start()
    {
        var session = await sessions.CreateAsync();
        var linkUrl = $"{_frontendUrl}/link-panel?token={session.Token}";

        ViewBag.Token    = session.Token;
        ViewBag.LinkUrl  = linkUrl;
        ViewBag.ExpiresAt = session.ExpiresAt.ToString("o");
        return View("AuthStart");
    }

    // Поллинг из AE панели
    [HttpGet("check")]
    public async Task<IActionResult> Check([FromQuery] string token)
    {
        var session = await sessions.GetByTokenAsync(token);

        if (session == null || session.IsExpired())
            return Ok(new { linked = false, expired = true });

        if (!session.IsLinked)
            return Ok(new
            {
                linked    = false,
                expired   = false,
                expiresAt = session.ExpiresAt.ToString("o")   // реальный остаток для таймера
            });

        // Подтверждено — ставим cookie.
        // Secure + SameSite=None нужны только на HTTPS (прод).
        // На HTTP (локалхост) ставим SameSite=Lax без Secure — иначе браузер отбросит куку.
        var isHttps  = HttpContext.Request.IsHttps;
        var jwt = jwtService.Generate(session.UserId!.Value);
        HttpContext.Response.Cookies.Append("autoslot_session", jwt, new CookieOptions
        {
            HttpOnly = true,
            Path     = "/",
            Secure   = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Expires  = DateTimeOffset.UtcNow.AddDays(7)
        });

        // Сразу сообщаем есть ли подписка — панель выберет куда редиректить
        var hasSubscription = await subscriptionService
            .IsActiveAsync(session.UserId!.Value, SubscriptionProductType.AutoSlot);

        return Ok(new { linked = true, hasSubscription });
    }

    // Вызывается с Astro /link-panel — проверяет сессию и отдаёт статус подписки
    [HttpGet("session-info")]
    public async Task<IActionResult> SessionInfo([FromQuery] string token)
    {
        var session = await sessions.GetByTokenAsync(token);

        if (session == null || session.IsExpired())
            return Ok(new { valid = false });

        return Ok(new
        {
            valid     = true,
            expiresAt = session.ExpiresAt.ToString("o")
        });
    }

    // Статус подписки для залогиненного пользователя — вызывается с Astro
    [HttpGet("subscription-status")]
    [Authorize]
    public async Task<IActionResult> SubscriptionStatus()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var hasSubscription = await subscriptionService
            .IsActiveAsync(userId, SubscriptionProductType.AutoSlot);

        return Ok(new { hasSubscription });
    }

    // Вызывается с Astro /link-panel после подтверждения
    [HttpPost("link")]
    [Authorize]
    public async Task<IActionResult> Link([FromQuery] string token)
    {
        var userId  = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var success = await sessions.LinkAsync(token, userId);
        return success ? Ok() : BadRequest("Token expired or already used");
    }

    // Выход из аккаунта и привязка нового — удаляет cookie и редиректит на экран линковки
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Response.Cookies.Delete("autoslot_session", new CookieOptions
        {
            Path     = "/",
            HttpOnly = true
        });
        return Redirect("/autoslot/auth/start");
    }
}

using Creohub.AutoSlot.Services;
using CreoHub.Application.Repositories;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Creohub.AutoSlot.Controllers;

[Route("autoslot")]
[EnableCors("AllowPanel")]
public class AutoSlotPanelController(SubscriptionService subs, IAccountRepository accounts, IConfiguration config) : Controller
{
    private readonly string _frontendUrl = config["Frontend"] ?? "https://www.creohub.xyz";
    // Основная панель — middleware уже проверил cookie + подписку
    [HttpGet("")]
    public IActionResult Index()
    {
        var userId = (Guid)HttpContext.Items["AutoSlotUserId"]!;
        ViewBag.UserId = userId;
        return View("Index");
    }

    // Страница покупки подписки (нет активной подписки)
    [HttpGet("subscribe")]
    public async Task<IActionResult> Subscribe()
    {
        var userId = (Guid)HttpContext.Items["AutoSlotUserId"]!;
        var user   = await accounts.GetFullInfoByIdAsync(userId);
        ViewBag.UserName    = user?.Name  ?? "Пользователь";
        ViewBag.UserEmail   = user?.EmailAddress ?? "";
        ViewBag.FrontendUrl = _frontendUrl;
        return View("Subscribe");
    }

    // Лёгкий ping: есть ли активная подписка у текущей сессии?
    // Вызывается поллингом со страницы /autoslot/subscribe
    [HttpGet("subscription-ping")]
    public async Task<IActionResult> SubscriptionPing()
    {
        var userId = (Guid)HttpContext.Items["AutoSlotUserId"]!;
        var active = await subs.IsActiveAsync(userId, CreoHub.Domain.Types.SubscriptionProductType.AutoSlot);
        return Ok(new { active });
    }

    // Данные профиля — статус подписки, дней осталось
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId  = (Guid)HttpContext.Items["AutoSlotUserId"]!;
        var product = CreoHub.Domain.Types.SubscriptionProductType.AutoSlot;

        // hasSubscription через IsActiveAsync — корректно учитывает пожизненную (ExpiresAt == null)
        var isActive   = await subs.IsActiveAsync(userId, product);
        var expiresAt  = await subs.GetLatestExpiresAtAsync(userId, product); // null у lifetime
        var isLifetime = isActive && expiresAt is null;
        int? daysLeft  = expiresAt.HasValue && expiresAt.Value > DateTime.UtcNow
            ? (int)Math.Ceiling((expiresAt.Value - DateTime.UtcNow).TotalDays)
            : null;

        return Ok(new
        {
            hasSubscription = isActive,
            isLifetime,
            expiresAt = expiresAt?.ToString("o"),
            daysLeft
        });
    }

    // Активация промо-кода из панели
    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemPromoCode([FromBody] RedeemRequest req)
    {
        var userId = (Guid)HttpContext.Items["AutoSlotUserId"]!;
        var success = await subs.RedeemPromoCodeAsync(userId, req.Code);
        return success ? Ok() : BadRequest("Код недействителен или уже использован");
    }

}

public record RedeemRequest(string Code);

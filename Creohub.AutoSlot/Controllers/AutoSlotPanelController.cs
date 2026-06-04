using Creohub.AutoSlot.Services;
using CreoHub.Application.Repositories;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Creohub.AutoSlot.Controllers;

[Route("autoslot")]
[EnableCors("AllowPanel")]
public class AutoSlotPanelController(SubscriptionService subs, IAccountRepository accounts) : Controller
{
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
        ViewBag.UserName  = user?.Name  ?? "Пользователь";
        ViewBag.UserEmail = user?.EmailAddress ?? "";
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
        var userId = (Guid)HttpContext.Items["AutoSlotUserId"]!;
        var expiresAt = await subs.GetLatestExpiresAtAsync(userId, CreoHub.Domain.Types.SubscriptionProductType.AutoSlot);
        var isActive  = expiresAt.HasValue && expiresAt.Value > DateTime.UtcNow;
        int? daysLeft = isActive
            ? (int)Math.Ceiling((expiresAt!.Value - DateTime.UtcNow).TotalDays)
            : null;

        return Ok(new
        {
            hasSubscription = isActive,
            expiresAt       = expiresAt?.ToString("o"),
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

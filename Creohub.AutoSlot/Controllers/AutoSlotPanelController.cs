using Creohub.AutoSlot.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Creohub.AutoSlot.Controllers;

[Route("autoslot")]
[EnableCors("AllowPanel")]
public class AutoSlotPanelController(SubscriptionService subs) : Controller
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
    public IActionResult Subscribe() => View("Subscribe");

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

    // Превью локального файла — читает GIF/PNG/WEBP с диска и отдаёт как image
    // Используется для отображения символов в панели без зависимости от CEP Node.js
    [HttpGet("local-preview")]
    public IActionResult LocalPreview([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest();

        // Нормализуем слеши
        path = path.Replace('/', System.IO.Path.DirectorySeparatorChar)
                   .Replace('\\', System.IO.Path.DirectorySeparatorChar);

        if (!System.IO.File.Exists(path))
            return NotFound();

        var ext  = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var mime = ext switch
        {
            ".gif"  => "image/gif",
            ".png"  => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        var bytes = System.IO.File.ReadAllBytes(path);
        return File(bytes, mime);
    }
}

public record RedeemRequest(string Code);

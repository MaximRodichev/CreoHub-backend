using System.Security.Claims;
using Creohub.AutoSlot.Services;
using CreoHub.Domain.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Creohub.AutoSlot.Controllers;

/// <summary>
/// REST-эндпоинты AutoSlot для Astro-фронта.
/// Аутентификация через Bearer JWT (стандартный Creohub JWT).
/// JSON-форма ответа: { status: 0, data } | { status: 1, errorMessage }
/// совпадает с BaseResponse из CreoHub.Application (без прямой зависимости).
/// </summary>
[ApiController]
[Route("api/autoslot")]
[Authorize]
public class AutoSlotApiController(SubscriptionService subscriptionService) : ControllerBase
{
    // Допустимые планы: дни → цена USD
    private static readonly Dictionary<int, decimal> Plans = new()
    {
        { 7,   5m  },
        { 30,  12m },
        { 180, 50m },
        { 365, 80m },
    };

    private Guid? UserId
    {
        get
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Статус подписки AutoSlot текущего пользователя.
    /// GET /api/autoslot/subscription
    /// </summary>
    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var isActive  = await subscriptionService.IsActiveAsync(userId.Value, SubscriptionProductType.AutoSlot);
        var expiresAt = await subscriptionService.GetLatestExpiresAtAsync(userId.Value, SubscriptionProductType.AutoSlot);

        int? daysLeft = expiresAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((expiresAt.Value - DateTime.UtcNow).TotalDays))
            : null;

        return Ok(new
        {
            status = 0,
            data = new
            {
                isActive,
                expiresAt,
                daysLeft,
                plan = isActive ? "AutoSlot Pro" : (string?)null,
            }
        });
    }

    /// <summary>
    /// Купить подписку с баланса пользователя.
    /// POST /api/autoslot/subscribe  — Body: { "days": 7|30|180|365 }
    /// </summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
    {
        var userId = UserId;
        if (userId == null) return Unauthorized();

        if (!Plans.TryGetValue(req.Days, out var price))
            return BadRequest(new { status = 1, errorMessage = "Неверный план подписки" });

        var (success, error) = await subscriptionService.PurchaseAsync(
            userId.Value, req.Days, price, SubscriptionProductType.AutoSlot);

        if (!success)
            return BadRequest(new { status = 1, errorMessage = error });

        return Ok(new
        {
            status = 0,
            data = new { message = "Подписка оформлена", days = req.Days, price }
        });
    }

    public record SubscribeRequest(int Days);
}

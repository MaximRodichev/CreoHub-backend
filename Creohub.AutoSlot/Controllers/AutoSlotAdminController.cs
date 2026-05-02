using Creohub.AutoSlot.Services;
using Creohub.Domain.Entities;
using CreoHub.Domain.Types;
using CreoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Creohub.AutoSlot.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin/autoslot")]
[ApiController]
public class AutoSlotAdminController(SubscriptionService subs, AppDbContext db) : ControllerBase
{
    [HttpPost("grant")]
    public async Task<IActionResult> Grant([FromBody] GrantSubscriptionRequest req)
    {
        await subs.GrantAsync(req.UserId, SubscriptionProductType.AutoSlot, req.Days, req.IsLifetime);
        return Ok();
    }

    [HttpPost("promo-codes")]
    public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeRequest req)
    {
        var code = SubscriptionPromoCode.Create(
            req.Code ?? GenerateCode(),
            SubscriptionProductType.AutoSlot,
            req.Days,
            req.IsLifetime,
            req.ExpiresAt);

        db.SubscriptionPromoCodes.Add(code);
        await db.SaveChangesAsync();
        return Ok(new { code.Code });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions([FromQuery] Guid? userId)
    {
        var query = db.Subscriptions.AsQueryable();
        if (userId.HasValue) query = query.Where(s => s.UserId == userId);
        var result = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return Ok(result);
    }

    [HttpGet("promo-codes")]
    public async Task<IActionResult> GetPromoCodes()
    {
        var result = await db.SubscriptionPromoCodes
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(result);
    }

    private static string GenerateCode() =>
        "AUTOSLOT-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
}

public record GrantSubscriptionRequest(Guid UserId, int Days, bool IsLifetime = false);
public record CreatePromoCodeRequest(string? Code, int Days, bool IsLifetime, DateTime? ExpiresAt);

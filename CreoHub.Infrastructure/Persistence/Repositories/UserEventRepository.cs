using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class UserEventRepository : IUserEventRepository
{
    private readonly AppDbContext _db;

    public UserEventRepository(AppDbContext db) => _db = db;

    // ── Write ─────────────────────────────────────────────────────────────────

    public async Task BulkInsertAsync(IEnumerable<UserEvent> events, CancellationToken ct = default)
    {
        await _db.UserEvents.AddRangeAsync(events, ct);
        await _db.SaveChangesAsync(ct);
    }

    // ── Product funnel ────────────────────────────────────────────────────────

    public async Task<Dictionary<string, int>> GetCountsByTypeForProductAsync(
        int productId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _db.UserEvents
            .Where(e => e.ProductId == productId && e.CreatedAt >= from && e.CreatedAt <= to)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count, ct);
    }

    // ── Shop analytics ────────────────────────────────────────────────────────

    public async Task<List<ProductEventStats>> GetProductStatsForShopAsync(
        Guid shopId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Join UserEvents with Products filtered by shop
        var productIds = await _db.Products
            .Where(p => p.OwnerId == shopId)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        if (productIds.Count == 0) return [];

        var ids = productIds.Select(p => p.Id).ToHashSet();

        var rawCounts = await _db.UserEvents
            .Where(e => e.ProductId != null
                     && ids.Contains(e.ProductId!.Value)
                     && e.CreatedAt >= from
                     && e.CreatedAt <= to
                     && (e.EventType == "product_view"
                      || e.EventType == "cart_add"
                      || e.EventType == "product_purchased"))
            .GroupBy(e => new { e.ProductId, e.EventType })
            .Select(g => new { g.Key.ProductId, g.Key.EventType, Count = g.Count() })
            .ToListAsync(ct);

        var nameMap = productIds.ToDictionary(p => p.Id, p => p.Name);

        return productIds.Select(p =>
        {
            int Get(string t) => rawCounts
                .Where(r => r.ProductId == p.Id && r.EventType == t)
                .Sum(r => r.Count);

            return new ProductEventStats(
                p.Id,
                p.Name,
                Views:     Get("product_view"),
                CartAdds:  Get("cart_add"),
                Purchases: Get("product_purchased"));
        }).ToList();
    }

    // ── Admin / platform ──────────────────────────────────────────────────────

    public async Task<Dictionary<string, int>> GetPlatformCountsByTypeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _db.UserEvents
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventType, x => x.Count, ct);
    }

    public async Task<List<TopSearchEntry>> GetTopSearchesAsync(
        DateTime from, DateTime to, int topN = 10, CancellationToken ct = default)
    {
        // Payload for search events is JSON: {"q":"..."}
        // Use raw SQL for JSON extraction to keep it efficient
        var rows = await _db.UserEvents
            .Where(e => e.EventType == "search"
                     && e.Payload   != null
                     && e.CreatedAt >= from
                     && e.CreatedAt <= to)
            .Select(e => e.Payload!)
            .ToListAsync(ct);

        // Parse JSON in-memory (volume is low)
        return rows
            .Select(p =>
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(p);
                    return doc.RootElement.TryGetProperty("q", out var q) ? q.GetString() : null;
                }
                catch { return null; }
            })
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .GroupBy(q => q!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(topN)
            .Select(g => new TopSearchEntry(g.Key, g.Count()))
            .ToList();
    }
}

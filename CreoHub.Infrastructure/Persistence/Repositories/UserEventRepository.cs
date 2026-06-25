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

    // ── Admin activity dashboard ────────────────────────────────────────────────

    public async Task<int> GetActiveUserCountAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await _db.UserEvents
            .Where(e => e.UserId != null && e.CreatedAt >= from && e.CreatedAt <= to)
            .Select(e => e.UserId!.Value)
            .Distinct()
            .CountAsync(ct);
    }

    public async Task<List<DailyEventCount>> GetDailyCountsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.UserEvents
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .GroupBy(e => new { Day = e.CreatedAt.Date, e.EventType })
            .Select(g => new { g.Key.Day, g.Key.EventType, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => new DailyEventCount(r.Day, r.EventType, r.Count)).ToList();
    }

    public async Task<List<ActivityEventRaw>> GetRecentActivityAsync(
        DateTime from, DateTime to, int take, CancellationToken ct = default)
    {
        var rows = await _db.UserEvents
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .Select(e => new { e.CreatedAt, e.EventType, e.UserId, e.ProductId, e.Payload })
            .ToListAsync(ct);

        return rows
            .Select(r => new ActivityEventRaw(r.CreatedAt, r.EventType, r.UserId, r.ProductId, r.Payload))
            .ToList();
    }

    public async Task<List<TopPageEntry>> GetTopPagesAsync(
        DateTime from, DateTime to, int topN = 15, CancellationToken ct = default)
    {
        // Payload for page_view events is JSON: {"path":"..."}
        var rows = await _db.UserEvents
            .Where(e => e.EventType == "page_view"
                     && e.Payload   != null
                     && e.CreatedAt >= from
                     && e.CreatedAt <= to)
            .Select(e => e.Payload!)
            .ToListAsync(ct);

        return rows
            .Select(p =>
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(p);
                    return doc.RootElement.TryGetProperty("path", out var path) ? path.GetString() : null;
                }
                catch { return null; }
            })
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(topN)
            .Select(g => new TopPageEntry(g.Key, g.Count()))
            .ToList();
    }

    // ── Retention / поведение ───────────────────────────────────────────────

    public async Task AttachSessionToUserAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"UserEvents\" SET \"UserId\"={0} WHERE \"SessionId\"={1} AND \"UserId\" IS NULL",
            new object[] { userId, sessionId }, ct);
    }

    public async Task<(List<SearchHistoryItem> Items, int Total)> GetSearchHistoryAsync(
        DateTime from, DateTime to, bool onlyNoResults, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.UserEvents.Where(e =>
            e.CreatedAt >= from && e.CreatedAt <= to &&
            (onlyNoResults
                ? e.EventType == "search_no_results"
                : (e.EventType == "search" || e.EventType == "search_no_results")));

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip(Math.Max(0, page) * pageSize)
            .Take(pageSize)
            .Select(e => new { e.CreatedAt, e.EventType, e.Payload, e.UserId, e.SessionId })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            var query = "";
            if (r.Payload != null)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(r.Payload);
                    if (doc.RootElement.TryGetProperty("q", out var qv)) query = qv.GetString() ?? "";
                }
                catch { /* битый payload — пустой запрос */ }
            }
            return new SearchHistoryItem(r.CreatedAt, query, r.EventType == "search_no_results", r.UserId, r.SessionId);
        }).ToList();

        return (items, total);
    }

    public async Task<List<FlowEventRaw>> GetSubjectFlowAsync(
        Guid? userId, string? sessionId, DateTime from, DateTime to, int take, CancellationToken ct = default)
    {
        var q = _db.UserEvents.Where(e => e.CreatedAt >= from && e.CreatedAt <= to);
        if (userId is Guid uid)
            q = q.Where(e => e.UserId == uid);
        else if (!string.IsNullOrWhiteSpace(sessionId))
            q = q.Where(e => e.SessionId == sessionId);
        else
            return [];

        var rows = await q
            .OrderBy(e => e.CreatedAt)
            .Take(take)
            .Select(e => new { e.CreatedAt, e.EventType, e.ProductId, e.Payload, e.SessionId, e.UserId })
            .ToListAsync(ct);

        return rows
            .Select(r => new FlowEventRaw(r.CreatedAt, r.EventType, r.ProductId, r.Payload, r.SessionId, r.UserId))
            .ToList();
    }
}

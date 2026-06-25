using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IUserEventRepository
{
    /// <summary>Bulk-insert many events in one round-trip. Used by the background batch writer.</summary>
    Task BulkInsertAsync(IEnumerable<UserEvent> events, CancellationToken ct = default);

    /// <summary>Aggregated counts per EventType for a single product (for funnel endpoint).</summary>
    Task<Dictionary<string, int>> GetCountsByTypeForProductAsync(
        int      productId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>Per-product aggregated stats for every product belonging to a shop.</summary>
    Task<List<ProductEventStats>> GetProductStatsForShopAsync(
        Guid     shopId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>Platform-wide event counts per type (admin dashboard).</summary>
    Task<Dictionary<string, int>> GetPlatformCountsByTypeAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    /// <summary>Top search queries ordered by frequency.</summary>
    Task<List<TopSearchEntry>> GetTopSearchesAsync(
        DateTime from,
        DateTime to,
        int      topN = 10,
        CancellationToken ct = default);

    // ── Admin activity dashboard ───────────────────────────────────────────────

    /// <summary>Distinct authenticated users that produced any event in the window.</summary>
    Task<int> GetActiveUserCountAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Per-day, per-type event counts (for trend chart).</summary>
    Task<List<DailyEventCount>> GetDailyCountsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Most recent events (newest first) for the live activity feed.</summary>
    Task<List<ActivityEventRaw>> GetRecentActivityAsync(
        DateTime from, DateTime to, int take, CancellationToken ct = default);

    /// <summary>Top visited paths from page_view events.</summary>
    Task<List<TopPageEntry>> GetTopPagesAsync(
        DateTime from, DateTime to, int topN = 15, CancellationToken ct = default);

    // ── Retention / поведение ───────────────────────────────────────────────

    /// <summary>Привязать анонимную историю (SessionId) к аккаунту при логине (стичинг гость→юзер).</summary>
    Task AttachSessionToUserAsync(Guid userId, string sessionId, CancellationToken ct = default);

    /// <summary>Пагинированная история поиска (search + search_no_results). onlyNoResults — только нулевые.</summary>
    Task<(List<SearchHistoryItem> Items, int Total)> GetSearchHistoryAsync(
        DateTime from, DateTime to, bool onlyNoResults, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Хронология событий субъекта (по UserId ИЛИ SessionId) — для flow-таймлайна.</summary>
    Task<List<FlowEventRaw>> GetSubjectFlowAsync(
        Guid? userId, string? sessionId, DateTime from, DateTime to, int take, CancellationToken ct = default);
}

public record ProductEventStats(
    int    ProductId,
    string ProductName,
    int    Views,
    int    CartAdds,
    int    Purchases);

public record TopSearchEntry(string Query, int Count);
public record TopPageEntry(string Path, int Count);
public record DailyEventCount(DateTime Day, string EventType, int Count);
public record ActivityEventRaw(DateTime At, string EventType, Guid? UserId, int? ProductId, string? Payload);
public record SearchHistoryItem(DateTime At, string Query, bool NoResults, Guid? UserId, string? SessionId);
public record FlowEventRaw(DateTime At, string EventType, int? ProductId, string? Payload, string? SessionId, Guid? UserId);

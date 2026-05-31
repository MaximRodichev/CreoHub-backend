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
}

public record ProductEventStats(
    int    ProductId,
    string ProductName,
    int    Views,
    int    CartAdds,
    int    Purchases);

public record TopSearchEntry(string Query, int Count);

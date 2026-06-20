using System.Text.Json;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.AnalyticsQueries;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetAdminActivityQuery(int Days = 14)
    : IRequest<BaseResponse<AdminActivityDTO>>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AdminActivityDTO(
    int Days,
    int NewUsersToday,
    int NewUsersInPeriod,
    int ActiveUsers,
    Dictionary<string, int>            EventCounts,
    IReadOnlyList<DailyDashboardPoint> Daily,
    IReadOnlyList<ActivityFeedItem>    RecentActivity,
    IReadOnlyList<TopSearchEntry>      TopSearches,
    IReadOnlyList<TopPageEntry>        TopPages);

public record DailyDashboardPoint(
    string Date,
    int    NewUsers,
    int    Views,
    int    Searches,
    int    CartAdds,
    int    Purchases,
    int    PageViews);

public record ActivityFeedItem(
    DateTime At,
    string   EventType,
    Guid?    UserId,
    string?  UserName,
    int?     ProductId,
    string?  ProductName,
    string?  Path);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetAdminActivityHandler
    : IRequestHandler<GetAdminActivityQuery, BaseResponse<AdminActivityDTO>>
{
    private readonly IUserEventRepository _events;
    private readonly IAdminRepository     _admin;

    public GetAdminActivityHandler(IUserEventRepository events, IAdminRepository admin)
    {
        _events = events;
        _admin  = admin;
    }

    public async Task<BaseResponse<AdminActivityDTO>> Handle(
        GetAdminActivityQuery request, CancellationToken ct)
    {
        try
        {
            var days       = Math.Clamp(request.Days, 1, 90);
            var to         = DateTime.UtcNow;
            var from       = to.AddDays(-days);
            var todayStart = to.Date;

            var counts      = await _events.GetPlatformCountsByTypeAsync(from, to, ct);
            var activeUsers = await _events.GetActiveUserCountAsync(from, to, ct);
            var dailyEvents = await _events.GetDailyCountsAsync(from, to, ct);
            var topSearches = await _events.GetTopSearchesAsync(from, to, 15, ct);
            var topPages    = await _events.GetTopPagesAsync(from, to, 15, ct);
            var recentRaw   = await _events.GetRecentActivityAsync(from, to, 60, ct);

            var newUsersPeriod = await _admin.GetRegisteredCountAsync(from, to, ct);
            var newUsersToday  = await _admin.GetRegisteredCountAsync(todayStart, to, ct);
            var dailyRegs      = await _admin.GetDailyRegistrationsAsync(from, to, ct);

            // ── Имена для ленты активности (batch-lookup) ──
            var userIds = recentRaw.Where(r => r.UserId    != null).Select(r => r.UserId!.Value).Distinct().ToList();
            var prodIds = recentRaw.Where(r => r.ProductId != null).Select(r => r.ProductId!.Value).Distinct().ToList();
            var userNames = await _admin.GetUserNamesAsync(userIds, ct);
            var prodNames = await _admin.GetProductNamesAsync(prodIds, ct);

            var feed = recentRaw.Select(r => new ActivityFeedItem(
                r.At,
                r.EventType,
                r.UserId,
                r.UserId is Guid uid && userNames.TryGetValue(uid, out var un) ? un : null,
                r.ProductId,
                r.ProductId is int pid && prodNames.TryGetValue(pid, out var pn) ? pn : null,
                ExtractPath(r.Payload, r.EventType))).ToList();

            // ── Сводная посуточная серия ──
            var regMap = dailyRegs.ToDictionary(d => d.Day.Date, d => d.Count);
            var evByDay = dailyEvents
                .GroupBy(d => d.Day.Date)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.EventType, x => x.Count));

            var daily = new List<DailyDashboardPoint>();
            for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
            {
                evByDay.TryGetValue(day, out var ev);
                int E(string t) => ev != null && ev.TryGetValue(t, out var c) ? c : 0;

                daily.Add(new DailyDashboardPoint(
                    day.ToString("yyyy-MM-dd"),
                    regMap.GetValueOrDefault(day, 0),
                    E(EventTypes.ProductView),
                    E(EventTypes.Search),
                    E(EventTypes.CartAdd),
                    E(EventTypes.ProductPurchased),
                    E(EventTypes.PageView)));
            }

            return BaseResponse<AdminActivityDTO>.Success(new AdminActivityDTO(
                days, newUsersToday, newUsersPeriod, activeUsers,
                counts, daily, feed, topSearches, topPages));
        }
        catch (Exception ex)
        {
            return BaseResponse<AdminActivityDTO>.Fail(ex.Message);
        }
    }

    /// <summary>Достаёт человекочитаемый путь/запрос из JSON-payload события.</summary>
    private static string? ExtractPath(string? payload, string eventType)
    {
        if (string.IsNullOrEmpty(payload)) return null;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (eventType == EventTypes.PageView && doc.RootElement.TryGetProperty("path", out var p))
                return p.GetString();
            if (eventType == EventTypes.Search && doc.RootElement.TryGetProperty("q", out var q))
                return q.GetString();
        }
        catch { /* malformed payload — игнорируем */ }
        return null;
    }
}

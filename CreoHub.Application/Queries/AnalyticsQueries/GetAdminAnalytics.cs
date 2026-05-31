using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.AnalyticsQueries;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetAdminAnalyticsQuery(int Days = 30)
    : IRequest<BaseResponse<AdminAnalyticsDTO>>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AdminAnalyticsDTO(
    int Days,
    Dictionary<string, int>  EventCounts,
    IReadOnlyList<TopSearchEntry> TopSearches);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetAdminAnalyticsHandler
    : IRequestHandler<GetAdminAnalyticsQuery, BaseResponse<AdminAnalyticsDTO>>
{
    private readonly IUserEventRepository _events;

    public GetAdminAnalyticsHandler(IUserEventRepository events) => _events = events;

    public async Task<BaseResponse<AdminAnalyticsDTO>> Handle(
        GetAdminAnalyticsQuery request, CancellationToken ct)
    {
        try
        {
            var to   = DateTime.UtcNow;
            var from = to.AddDays(-request.Days);

            var counts  = await _events.GetPlatformCountsByTypeAsync(from, to, ct);
            var topSearch = await _events.GetTopSearchesAsync(from, to, 15, ct);

            return BaseResponse<AdminAnalyticsDTO>.Success(
                new AdminAnalyticsDTO(request.Days, counts, topSearch));
        }
        catch (Exception ex)
        {
            return BaseResponse<AdminAnalyticsDTO>.Fail(ex.Message);
        }
    }
}

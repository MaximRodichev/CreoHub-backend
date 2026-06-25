using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record SearchHistoryEntryDto(
    DateTime At, string Query, bool NoResults, Guid? UserId, string? UserName, string? SessionId);

public record SearchHistoryDto(
    IReadOnlyList<SearchHistoryEntryDto> Items, int Total, int Page, int PageSize);

/// <summary>История поиска для админки: пагинация + фильтр «только без результатов» (бэклог спроса).</summary>
public record GetSearchHistoryQuery(int Days = 30, bool OnlyNoResults = false, int Page = 0, int PageSize = 50)
    : IRequest<BaseResponse<SearchHistoryDto>>;

public class GetSearchHistoryHandler : IRequestHandler<GetSearchHistoryQuery, BaseResponse<SearchHistoryDto>>
{
    private readonly IUserEventRepository _events;
    private readonly IAdminRepository     _admin;

    public GetSearchHistoryHandler(IUserEventRepository events, IAdminRepository admin)
    {
        _events = events;
        _admin  = admin;
    }

    public async Task<BaseResponse<SearchHistoryDto>> Handle(GetSearchHistoryQuery request, CancellationToken ct)
    {
        try
        {
            var to       = DateTime.UtcNow;
            var from     = to.AddDays(-Math.Clamp(request.Days, 1, 365));
            var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var page     = Math.Max(0, request.Page);

            var (items, total) = await _events.GetSearchHistoryAsync(from, to, request.OnlyNoResults, page, pageSize, ct);

            var userIds = items.Where(i => i.UserId.HasValue).Select(i => i.UserId!.Value).Distinct().ToList();
            var names   = userIds.Count > 0
                ? await _admin.GetUserNamesAsync(userIds, ct)
                : new Dictionary<Guid, string>();

            var dto = new SearchHistoryDto(
                items.Select(i => new SearchHistoryEntryDto(
                    i.At, i.Query, i.NoResults, i.UserId,
                    i.UserId.HasValue ? names.GetValueOrDefault(i.UserId.Value) : null,
                    i.SessionId)).ToList(),
                total, page, pageSize);

            return BaseResponse<SearchHistoryDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return BaseResponse<SearchHistoryDto>.Fail(ex.Message);
        }
    }
}

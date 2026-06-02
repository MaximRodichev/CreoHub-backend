using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopRequestDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.ShopRequests;

// ── Список запросов магазина (продавец) ─────────────────────────────────────────

public record GetShopRequestsQuery(Guid ShopId, ShopRequestStatus? Status, int Page, int PageSize)
    : IRequest<BaseResponse<List<ShopRequestDTO>>>;

public class GetShopRequestsHandler
    : IRequestHandler<GetShopRequestsQuery, BaseResponse<List<ShopRequestDTO>>>
{
    private readonly IShopRequestRepository _requests;

    public GetShopRequestsHandler(IShopRequestRepository requests) => _requests = requests;

    public async Task<BaseResponse<List<ShopRequestDTO>>> Handle(
        GetShopRequestsQuery request, CancellationToken ct)
    {
        var page     = request.Page     < 1 ? 1  : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 30 : request.PageSize;

        var items = await _requests.GetByShopIdAsync(request.ShopId, request.Status, page, pageSize, ct);
        return BaseResponse<List<ShopRequestDTO>>.Success(items);
    }
}

// ── Кол-во новых запросов (бейдж) ───────────────────────────────────────────────

public record GetUnreadShopRequestCountQuery(Guid ShopId) : IRequest<BaseResponse<int>>;

public class GetUnreadShopRequestCountHandler
    : IRequestHandler<GetUnreadShopRequestCountQuery, BaseResponse<int>>
{
    private readonly IShopRequestRepository _requests;

    public GetUnreadShopRequestCountHandler(IShopRequestRepository requests) => _requests = requests;

    public async Task<BaseResponse<int>> Handle(GetUnreadShopRequestCountQuery request, CancellationToken ct)
    {
        var count = await _requests.CountNewByShopIdAsync(request.ShopId, ct);
        return BaseResponse<int>.Success(count);
    }
}

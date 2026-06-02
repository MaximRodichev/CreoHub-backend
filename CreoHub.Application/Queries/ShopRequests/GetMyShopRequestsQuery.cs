using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopRequestDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.ShopRequests;

// ── Список предложений покупателя («Мои предложения») ───────────────────────────

public record GetMyShopRequestsQuery(Guid BuyerUserId, int Page, int PageSize)
    : IRequest<BaseResponse<List<MyShopRequestDTO>>>;

public class GetMyShopRequestsHandler
    : IRequestHandler<GetMyShopRequestsQuery, BaseResponse<List<MyShopRequestDTO>>>
{
    private readonly IShopRequestRepository _requests;

    public GetMyShopRequestsHandler(IShopRequestRepository requests) => _requests = requests;

    public async Task<BaseResponse<List<MyShopRequestDTO>>> Handle(
        GetMyShopRequestsQuery request, CancellationToken ct)
    {
        var page     = request.Page     < 1 ? 1  : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 30 : request.PageSize;

        var items = await _requests.GetByBuyerIdAsync(request.BuyerUserId, page, pageSize, ct);
        return BaseResponse<List<MyShopRequestDTO>>.Success(items);
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.ShopFollows;

public record GetFollowStatusQuery(Guid UserId, Guid ShopId) : IRequest<BaseResponse<bool>>;
public record GetShopFollowerCountQuery(Guid ShopId)          : IRequest<BaseResponse<int>>;

// ── Подписан ли пользователь на магазин ──────────────────────────────────────

public class GetFollowStatusHandler : IRequestHandler<GetFollowStatusQuery, BaseResponse<bool>>
{
    private readonly IShopFollowRepository _follows;
    public GetFollowStatusHandler(IShopFollowRepository follows) => _follows = follows;

    public async Task<BaseResponse<bool>> Handle(GetFollowStatusQuery request, CancellationToken ct)
    {
        try
        {
            var following = await _follows.IsFollowingAsync(request.UserId, request.ShopId, ct);
            return BaseResponse<bool>.Success(following);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

// ── Кол-во подписчиков магазина (для дашборда владельца) ──────────────────────

public class GetShopFollowerCountHandler : IRequestHandler<GetShopFollowerCountQuery, BaseResponse<int>>
{
    private readonly IShopFollowRepository _follows;
    public GetShopFollowerCountHandler(IShopFollowRepository follows) => _follows = follows;

    public async Task<BaseResponse<int>> Handle(GetShopFollowerCountQuery request, CancellationToken ct)
    {
        try
        {
            var count = await _follows.CountFollowersAsync(request.ShopId, ct);
            return BaseResponse<int>.Success(count);
        }
        catch (Exception ex)
        {
            return BaseResponse<int>.Fail(ex.Message);
        }
    }
}

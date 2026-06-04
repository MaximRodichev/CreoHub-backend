using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ShopFollows;

public record FollowShopCommand(Guid UserId, Guid ShopId)   : IRequest<BaseResponse<bool>>;
public record UnfollowShopCommand(Guid UserId, Guid ShopId) : IRequest<BaseResponse<bool>>;

// ── Follow ───────────────────────────────────────────────────────────────────

public class FollowShopHandler : IRequestHandler<FollowShopCommand, BaseResponse<bool>>
{
    private readonly IShopFollowRepository _follows;
    private readonly IShopRepository       _shops;
    private readonly IUnitOfWork           _unitOfWork;

    public FollowShopHandler(
        IShopFollowRepository follows,
        IShopRepository       shops,
        IUnitOfWork           unitOfWork)
    {
        _follows    = follows;
        _shops      = shops;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(FollowShopCommand request, CancellationToken ct)
    {
        try
        {
            var shop = await _shops.GetShopByIdAsync(request.ShopId);
            if (shop is null)
                return BaseResponse<bool>.Fail("Магазин не найден.");
            if (shop.OwnerId == request.UserId)
                return BaseResponse<bool>.Fail("Нельзя подписаться на собственный магазин.");

            // Идемпотентно: уже подписан — это успех, без дублей
            if (await _follows.IsFollowingAsync(request.UserId, request.ShopId, ct))
                return BaseResponse<bool>.Success(true);

            await _follows.AddAsync(ShopFollow.Create(request.UserId, request.ShopId), ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

// ── Unfollow ─────────────────────────────────────────────────────────────────

public class UnfollowShopHandler : IRequestHandler<UnfollowShopCommand, BaseResponse<bool>>
{
    private readonly IShopFollowRepository _follows;

    public UnfollowShopHandler(IShopFollowRepository follows) => _follows = follows;

    public async Task<BaseResponse<bool>> Handle(UnfollowShopCommand request, CancellationToken ct)
    {
        try
        {
            // Идемпотентно: ExecuteDelete не падает если подписки нет
            await _follows.RemoveAsync(request.UserId, request.ShopId, ct);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

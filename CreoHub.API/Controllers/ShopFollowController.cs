using CreoHub.Application.Commands.ShopFollows;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.ShopFollows;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopFollowController : ShopOwnerControllerBase
{
    private readonly IMediator       _mediator;
    private readonly IShopRepository _shopRepository;

    public ShopFollowController(IMediator mediator, IShopRepository shopRepository)
    {
        _mediator       = mediator;
        _shopRepository = shopRepository;
    }

    // ── Покупатель: подписаться ─────────────────────────────────────────────────

    [Authorize]
    [HttpPost("{shopId:guid}")]
    public async Task<IActionResult> Follow(Guid shopId)
    {
        var response = await _mediator.Send(new FollowShopCommand(UserId, shopId));
        return Ok(response);
    }

    // ── Покупатель: отписаться ──────────────────────────────────────────────────

    [Authorize]
    [HttpDelete("{shopId:guid}")]
    public async Task<IActionResult> Unfollow(Guid shopId)
    {
        var response = await _mediator.Send(new UnfollowShopCommand(UserId, shopId));
        return Ok(response);
    }

    // ── Покупатель: подписан ли я на этот магазин ───────────────────────────────

    [Authorize]
    [HttpGet("{shopId:guid}/status")]
    public async Task<IActionResult> Status(Guid shopId)
    {
        var response = await _mediator.Send(new GetFollowStatusQuery(UserId, shopId));
        return Ok(response);
    }

    // ── Владелец: счётчик подписчиков своего магазина ───────────────────────────

    [Authorize]
    [HttpGet("my-shop/count")]
    public async Task<IActionResult> MyShopFollowerCount()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return Ok(BaseResponse<int>.Success(0));

        var response = await _mediator.Send(new GetShopFollowerCountQuery(shopId));
        return Ok(response);
    }
}

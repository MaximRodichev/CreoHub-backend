using CreoHub.Application.Commands.ShopRequests;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.ShopRequests;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopRequestController : ShopOwnerControllerBase
{
    private readonly IMediator       _mediator;
    private readonly IShopRepository _shopRepository;

    public ShopRequestController(IMediator mediator, IShopRepository shopRepository)
    {
        _mediator       = mediator;
        _shopRepository = shopRepository;
    }

    // ── Покупатель: создать предложение ─────────────────────────────────────────

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShopRequestDTO dto)
    {
        var response = await _mediator.Send(
            new CreateShopRequestCommand(UserId, dto.ShopId, dto.Message ?? string.Empty));
        return Ok(response);
    }

    // ── Покупатель: «Мои предложения» ───────────────────────────────────────────

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> My([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var response = await _mediator.Send(new GetMyShopRequestsQuery(UserId, page, pageSize));
        return Ok(response);
    }

    // ── Продавец: список запросов магазина ──────────────────────────────────────

    [Authorize]
    [HttpGet("shop")]
    public async Task<IActionResult> ShopList(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        ShopRequestStatus? parsed =
            Enum.TryParse<ShopRequestStatus>(status, ignoreCase: true, out var s) ? s : null;

        var response = await _mediator.Send(new GetShopRequestsQuery(shopId, parsed, page, pageSize));
        return Ok(response);
    }

    // ── Продавец: бейдж (кол-во новых) ──────────────────────────────────────────

    [Authorize]
    [HttpGet("shop/unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return Ok(BaseResponse<int>.Success(0));

        var response = await _mediator.Send(new GetUnreadShopRequestCountQuery(shopId));
        return Ok(response);
    }

    // ── Продавец: ответить ──────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("{id:int}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyShopRequestDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(
            new ReplyToShopRequestCommand(shopId, id, dto.Text ?? string.Empty));
        return Ok(response);
    }

    // ── Продавец: отклонить ─────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> Decline(int id, [FromBody] DeclineShopRequestDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(
            new DeclineShopRequestCommand(shopId, id, dto.Reason));
        return Ok(response);
    }
}

public record CreateShopRequestDTO(Guid ShopId, string? Message);
public record ReplyShopRequestDTO(string? Text);
public record DeclineShopRequestDTO(string? Reason);

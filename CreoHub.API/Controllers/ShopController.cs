using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Queries.Shop;
using CreoHub.Application.Queries.Tag;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopController : ShopOwnerControllerBase
{
    private readonly IMediator       _mediator;
    private readonly IShopRepository _shopRepository;

    public ShopController(IMediator mediator, IShopRepository shopRepository)
    {
        _mediator       = mediator;
        _shopRepository = shopRepository;
    }

    // ── Create shop ───────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateShopDTO dto)
    {
        var command  = new CreateShopCommand(UserId, dto);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Products ──────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetProductsExtendViewsQuery(shopId));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("products-list-names")]
    public async Task<IActionResult> GetProductsListNames()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetProductsNameByShopIdQuery(shopId));
        return Ok(response);
    }

    // ── Clients ───────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("clients-list-names")]
    public async Task<IActionResult> GetClientsListNames()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetClientsNameQuery(shopId));
        if (response.Status == ResponseStatus.Error)
            return BadRequest(response.ErrorMessage);

        return Ok(response);
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("tags-stats")]
    public async Task<IActionResult> GetTagsStats()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetTagStatsByShopQuery(shopId));
        if (response.Status == ResponseStatus.Error)
            return BadRequest(response.ErrorMessage);

        return Ok(response);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetShopDashboardQuery(shopId, from, to));
        return Ok(response);
    }

    // ── Balance ───────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetShopBalanceQuery(shopId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(
            new WithdrawShopBalanceCommand(shopId, dto.Amount, dto.Address, dto.Network));
        return Ok(response);
    }

    // ── Transactions ──────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetShopTransactionsQuery(shopId));
        return Ok(response);
    }
}

public record WithdrawRequestDTO(decimal Amount, string Address, string Network);

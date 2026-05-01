using System.Security.Claims;
using System.Text.Json;
using CreoHub.API.Models;
using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Queries.Shop;
using CreoHub.Application.Queries.Tag;
using CreoHub.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShopController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JwtService _jwtService;

    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    protected Guid ShopId => Guid.Parse(User.FindFirst("shop_id")?.Value ?? Guid.Empty.ToString());

    public ShopController(IMediator mediator, JwtService jwtService)
    {
        _mediator = mediator;
        _jwtService = jwtService;
    }

    [Authorize]
    [HttpPost("create")]
    public IActionResult Create([FromBody] CreateShopDTO dto)
    {
        return BadRequest(BaseResponse<string>.Fail("В разработке"));
    }

    [Authorize]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var command = new GetProductsExtendViewsQuery(ShopId);
        var response = await _mediator.Send(command);
        
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("products-list-names")]
    public async Task<IActionResult> GetProductsListNames()
    {
        Guid shopId = Guid.Parse(User.FindFirst("shop_id").Value);
        var command = new GetProductsNameByShopIdQuery(shopId);
        var response = await _mediator.Send(command);
        
        
        return Ok(response);
    }
    
    [Authorize]
    [HttpGet("clients-list-names")]
    public async Task<IActionResult> GetClientsListNames()
    {
        Guid shopId = Guid.Parse(User.FindFirst("shop_id").Value);
        var command = new GetClientsNameQuery(shopId);
        var response = await _mediator.Send(command);
        
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("tags-stats")]
    public async Task<IActionResult> GetTagsStats()
    {
        var command = new GetTagStatsByShopQuery(ShopId);
        var response = await _mediator.Send(command);
        
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var command = new GetShopDashboardQuery(ShopId, from, to);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Баланс магазина (заработанные средства после комиссии платформы).
    /// </summary>
    [Authorize]
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var response = await _mediator.Send(new GetShopBalanceQuery(ShopId));
        return Ok(response);
    }

    /// <summary>
    /// Вывод средств на крипто-кошелёк через OxaPay.
    /// </summary>
    [Authorize]
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequestDTO dto)
    {
        var response = await _mediator.Send(
            new WithdrawShopBalanceCommand(ShopId, dto.Amount, dto.Address, dto.Network));
        return Ok(response);
    }
}

public record WithdrawRequestDTO(decimal Amount, string Address, string Network);
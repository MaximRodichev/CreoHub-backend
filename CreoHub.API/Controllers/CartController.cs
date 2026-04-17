using System.Security.Claims;
using CreoHub.Application.Commands.CartCommands;
using CreoHub.Application.DTO.CartDTOs;
using CreoHub.Application.Queries.Cart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")] 
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    
    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    public CartController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [Authorize]
    [HttpGet("my-cart")]
    public async Task<IActionResult> GetCart()
    {
        var response = await _mediator.Send(new GetCartQuery(UserId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("toggle-item")]
    public async Task<IActionResult> ToggleCartItem([FromQuery] int productId)
    {
        var response = await _mediator.Send(new ToggleCartItemQuery(UserId, productId));
        return Ok(response);
    }

    /// <summary>
    /// Обновить выбранные файлы в CartItem (замена всего списка).
    /// Body: { "fileIds": ["guid1", "guid2"] } — пустой массив = полная покупка.
    /// </summary>
    [Authorize]
    [HttpPut("item/{cartItemId}/files")]
    public async Task<IActionResult> UpdateCartItemFiles(
        [FromRoute] Guid cartItemId,
        [FromBody] UpdateCartItemFilesDTO dto)
    {
        var response = await _mediator.Send(
            new UpdateCartItemFilesCommand(UserId, cartItemId, dto.FileIds));
        return Ok(response);
    }

    /// <summary>
    /// Полностью очистить корзину.
    /// </summary>
    [Authorize]
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var response = await _mediator.Send(new ClearCartCommand(UserId));
        return Ok(response);
    }
}
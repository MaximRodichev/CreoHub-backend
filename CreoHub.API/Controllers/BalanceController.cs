using System.Security.Claims;
using CreoHub.Application.Commands.BalanceCommands;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO.BalanceDTOs;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Balance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BalanceController : ControllerBase
{
    private readonly IMediator _mediator;

    protected Guid UserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    public BalanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Получить текущий баланс пользователя.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBalance()
    {
        var response = await _mediator.Send(new GetBalanceQuery(UserId));
        return Ok(response);
    }

    /// <summary>
    /// Создать инвойс OxaPay для пополнения баланса.
    /// Возвращает ссылку на оплату и trackId.
    /// </summary>
    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpBalanceRequest request)
    {
        var response = await _mediator.Send(new TopUpBalanceCommand(UserId, request.Amount));
        return Ok(response);
    }

    /// <summary>
    /// Оплатить заказ с внутреннего баланса (Path B — без редиректа на OxaPay).
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutWithBalance([FromBody] List<CheckoutItemDTO> items)
    {
        var sid = Request.Headers.TryGetValue("X-Session-Id", out var sv) ? sv.ToString() : null;
        var response = await _mediator.Send(new CheckoutWithBalanceCommand(UserId, items, sid));
        return Ok(response);
    }
}

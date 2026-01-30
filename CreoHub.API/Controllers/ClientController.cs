using System.Security.Claims;
using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Queries.Shop;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet]
    [Route("get-all")]
    public async Task<IActionResult> GetClients()
    {
        Guid shopId = Guid.Parse(User.FindFirst("shop_id").Value);
        var response = await _mediator.Send(new GetClientsShortInfoQuery(shopId));

        return Ok(response);
    }
    
    //TODO: временный код
    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateClient([FromQuery] string username, string telegramId, string telegramUsername, DateTime registrationDate)
    {
        var userData = new AuthAccountDTO
        {
            Name = username,
            Email = null,
            TelegramId = Int64.Parse(telegramId),
            TelegramUsername = telegramUsername
        };

        var response = await _mediator.Send(new AuthAccountCommand(userData, registrationDate));

        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        return Ok(response);
    }
}
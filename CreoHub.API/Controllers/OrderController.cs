using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDevDTO request)
    {
        
        var command = new CreateOrderDevCommand(request);
        var response =  await _mediator.Send(command);

        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }

        return Ok(response);
    }

    [Authorize]
    [HttpGet]
    [Route("get-shortinfo-list")]
    public async Task<IActionResult> Get()
    {
        Guid shopId = Guid.Parse(User.FindFirst("shop_id").Value);
        var query = new GetOrdersShortInfoByShopIdQuery(shopId);
        var response = await _mediator.Send(query);

        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        return Ok(response);
    }
}
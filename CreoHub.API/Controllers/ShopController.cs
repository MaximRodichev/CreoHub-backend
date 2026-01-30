using System.Security.Claims;
using System.Text.Json;
using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Queries.Shop;
using CreoHub.Application.Queries.Tag;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")] 
public class ShopController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ShopController(IMediator mediator)
    {
        _mediator=mediator;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody]  CreateShopDTO dto)
    {
        Guid id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var command = new CreateShopCommand(id, dto);
        var response = await _mediator.Send(command);
        
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] FiltersDto dto)
    {
        Guid id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var command = new GetProductsByFilterQuery(dto);
        var response = await _mediator.Send(command);
        
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("products-list-names")]
    public async Task<IActionResult> GetProductsListNames()
    {
        Guid shopId = Guid.Parse(User.FindFirst("shop_id").Value);
        var command = new GetProductsNameByShopIdQuery(shopId);
        var response = await _mediator.Send(command);
        
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        
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
}
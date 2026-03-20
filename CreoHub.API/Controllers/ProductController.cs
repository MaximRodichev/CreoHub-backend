using System.Security.Claims;
using AutoMapper;
using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProductRepository _productRepository;
    
    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    protected Guid ShopId => Guid.Parse(User.FindFirst("shop_id")?.Value ?? Guid.Empty.ToString());

    public ProductController(IMediator mediator, IProductRepository productRepository)
    {
        _mediator = mediator;
        _productRepository = productRepository;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProductDTO dto)
    {
        Guid id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var command = new CreateProductCommand(id, dto);
        var response = await _mediator.Send(command);
        
        if(response.Status== ResponseStatus.Error)
            return BadRequest(response.ErrorMessage);
        
        return Ok(response);
    }
    
    
    [Authorize]
    [HttpPost("create-bundle")]
    public async Task<IActionResult> Create([FromBody] CreateProductBundleDTO dto)
    {
        var command = new CreateProductBundleCommand(UserId, dto);
        var response = await _mediator.Send(command);
        

        return Ok(response);
    }

    [HttpGet("get-products")]
    public async Task<IActionResult> GetProducts([FromQuery] FiltersDto filters)
    {
        var command = new GetProductsByFilterQuery(filters);
        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [HttpGet("get-product-info")]
    public async Task<IActionResult> GetProductInfo([FromQuery] string name)
    {
        var command = new GetProductInfoByNameQuery(name);
        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("get-product-analytics")]
    public async Task<IActionResult> GetProductAnalytics([FromQuery] int productId)
    {
        var command = new GetProductAnalyticsQuery(ShopId, productId);
        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [Authorize]
    [HttpPost("update")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductInfoDTO dto)
    {
        var command = new UpdateProductCommand(ShopId, dto);
        var response = await _mediator.Send(command);
        
        return Ok(response);
    }
}
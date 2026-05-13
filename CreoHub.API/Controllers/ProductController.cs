using System.Security.Claims;
using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ShopOwnerControllerBase
{
    private readonly IMediator           _mediator;
    private readonly IProductRepository  _productRepository;
    private readonly IShopRepository     _shopRepository;

    public ProductController(IMediator mediator, IProductRepository productRepository, IShopRepository shopRepository)
    {
        _mediator           = mediator;
        _productRepository  = productRepository;
        _shopRepository     = shopRepository;
    }

    // ── Public endpoints (no shop needed) ────────────────────────────────────

    [HttpGet("get-products")]
    public async Task<IActionResult> GetProducts([FromQuery] FiltersDto filters)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var uid) && uid != Guid.Empty)
            filters = filters with { UserId = uid };

        var response = await _mediator.Send(new GetProductsByFilterQuery(filters));
        return Ok(response);
    }

    [HttpGet("best")]
    public async Task<IActionResult> GetBestProducts()
    {
        var response = await _mediator.Send(new GetBestProductsQuery());
        return Ok(response);
    }

    [HttpGet("get-product-info")]
    public async Task<IActionResult> GetProductInfo([FromQuery] string name)
    {
        var response = await _mediator.Send(new GetProductInfoByNameQuery(name));
        return Ok(response);
    }

    [HttpGet("{id:int}/info")]
    public async Task<IActionResult> GetProductInfoById([FromRoute] int id)
    {
        var response = await _mediator.Send(new GetProductInfoByIdQuery(id));
        return Ok(response);
    }

    [HttpGet("{id}/content-files")]
    public async Task<IActionResult> GetContentFiles([FromRoute] int id)
    {
        var response = await _mediator.Send(new GetProductContentFilesQuery(id));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("ownership")]
    public async Task<IActionResult> GetOwnership()
    {
        var response = await _mediator.Send(new GetProductOwnershipQuery(UserId));
        return Ok(response);
    }

    // ── Shop owner endpoints (ShopId resolved from DB) ────────────────────────

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProductDTO dto)
    {
        var command  = new CreateProductCommand(UserId, dto);
        var response = await _mediator.Send(command);
        // Always return JSON so the client can parse the response uniformly
        return Ok(response);
    }

    [Authorize]
    [HttpPost("create-bundle")]
    public async Task<IActionResult> CreateBundle([FromBody] CreateProductBundleDTO dto)
    {
        var response = await _mediator.Send(new CreateProductBundleCommand(UserId, dto));
        return Ok(response);
    }

    [Authorize]
    [HttpGet("get-product-analytics")]
    public async Task<IActionResult> GetProductAnalytics([FromQuery] int productId)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetProductAnalyticsQuery(shopId, productId));
        return Ok(response);
    }

    [Authorize]
    [HttpPost("update")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductInfoDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new UpdateProductCommand(shopId, dto));
        return Ok(response);
    }

    [Authorize]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] int id, [FromBody] ChangeProductStatusDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new ChangeProductStatusCommand(shopId, id, dto.TargetStatus));
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct([FromRoute] int id)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new DeleteProductCommand(shopId, id));
        return Ok(response);
    }
}

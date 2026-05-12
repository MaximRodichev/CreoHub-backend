using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ShopOwnerControllerBase
{
    private readonly IMediator       _mediator;
    private readonly IShopRepository _shopRepository;

    public OrderController(IMediator mediator, IShopRepository shopRepository)
    {
        _mediator       = mediator;
        _shopRepository = shopRepository;
    }

    [Authorize]
    [HttpGet("get-shortinfo-list")]
    public async Task<IActionResult> Get()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetOrdersShortInfoByShopIdQuery(shopId));
        return Ok(response);
    }
}

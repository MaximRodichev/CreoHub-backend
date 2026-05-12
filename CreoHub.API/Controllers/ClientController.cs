using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Shop;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ShopOwnerControllerBase
{
    private readonly IMediator       _mediator;
    private readonly IShopRepository _shopRepository;

    public ClientController(IMediator mediator, IShopRepository shopRepository)
    {
        _mediator       = mediator;
        _shopRepository = shopRepository;
    }

    [Authorize]
    [HttpGet("get-all")]
    public async Task<IActionResult> GetClients()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new GetClientsShortInfoQuery(shopId));
        return Ok(response);
    }
}

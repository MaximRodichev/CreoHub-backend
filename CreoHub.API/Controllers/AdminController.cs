using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Admin;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Queries.Shop;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

/// <summary>
/// Все эндпоинты доступны только для аккаунта icreoaffilate@gmail.com.
/// Проверка — по email-клейму в JWT (claim "email").
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private const string AdminEmail = "icreoaffilate@gmail.com";

    public AdminController(IMediator mediator) => _mediator = mediator;

    private IActionResult Forbidden() =>
        StatusCode(403, new { error = "Доступ запрещён. Требуется аккаунт администратора." });

    private bool IsAdmin()
    {
        var emailaddress = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return emailaddress == AdminEmail;
    }

    // ══════════════════════════════════════════
    // ПОЛЬЗОВАТЕЛИ
    // ══════════════════════════════════════════

    /// <summary>Список всех пользователей с базовой статистикой.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetAdminUsersQuery());
        return Ok(result);
    }

    /// <summary>Полный профиль пользователя + последние 50 заказов.</summary>
    [HttpGet("user/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetAdminUserDetailQuery(id));
        return Ok(result);
    }

    /// <summary>Создать клиента вручную (без регистрации через OAuth).</summary>
    [HttpPost("user")]
    public async Task<IActionResult> CreateClient([FromBody] AdminCreateClientDto dto)
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new CreateClientCommand(dto.Name, dto.Email));
        return result.Status == Application.DTO.ResponseStatus.Success
            ? Ok(result)
            : BadRequest(result);
    }

    // ══════════════════════════════════════════
    // МАГАЗИНЫ
    // ══════════════════════════════════════════

    /// <summary>Все магазины с выручкой и количеством продуктов.</summary>
    [HttpGet("shops")]
    public async Task<IActionResult> GetShops()
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetAdminShopsQuery());
        return Ok(result);
    }

    /// <summary>Дашборд конкретного магазина (те же данные что видит владелец).</summary>
    [HttpGet("shop/{shopId:guid}/dashboard")]
    public async Task<IActionResult> GetShopDashboard(
        Guid shopId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetShopDashboardQuery(shopId, from, to));
        return Ok(result);
    }

    /// <summary>Заказы конкретного магазина.</summary>
    [HttpGet("shop/{shopId:guid}/orders")]
    public async Task<IActionResult> GetShopOrders(
        Guid shopId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetOrdersShortInfoByShopIdQuery(shopId, from, to));
        return Ok(result);
    }

    /// <summary>Клиенты (покупатели) конкретного магазина.</summary>
    [HttpGet("shop/{shopId:guid}/clients")]
    public async Task<IActionResult> GetShopClients(Guid shopId)
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetClientsShortInfoQuery(shopId));
        return Ok(result);
    }

    // ══════════════════════════════════════════
    // ПРОДУКТЫ / КЛИЕНТЫ (автокомплит в форме)
    // ══════════════════════════════════════════

    /// <summary>Список всех продуктов (id + name) для автокомплита при создании заказа.</summary>
    [HttpGet("products-list-names")]
    public async Task<IActionResult> GetProductsListNames()
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new GetAdminProductsListQuery());
        return Ok(result);
    }

    // ══════════════════════════════════════════
    // ЗАКАЗЫ
    // ══════════════════════════════════════════

    /// <summary>
    /// Создать заказ вручную (без транзакции/оплаты).
    /// Используется для бухгалтерии: перенос продаж из переписки в систему.
    /// </summary>
    [HttpPost("order")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDevDTO dto)
    {
        if (!IsAdmin()) return Forbidden();
        var result = await _mediator.Send(new CreateOrderDevCommand(dto));
        return result.Status == Application.DTO.ResponseStatus.Success
            ? Ok(result)
            : BadRequest(result);
    }
}

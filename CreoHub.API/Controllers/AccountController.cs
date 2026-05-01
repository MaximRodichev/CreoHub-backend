using System.Net;
using System.Security.Claims;
using CreoHub.API.Models;
using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Queries.Content;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Queries.Product;
using CreoHub.Domain.Entities;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")] 
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;
    
    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    protected Guid ShopId => Guid.Parse(User.FindFirst("shop_id")?.Value ?? Guid.Empty.ToString());
    
    public AccountController(IMediator mediator,  JwtService jwtService, IConfiguration configuration)
    {
        _mediator=mediator;
        _jwtService=jwtService;
        _configuration=configuration;
    }
    
    [HttpGet("auth/google-signin")]
    public IActionResult LoginGoogle()
    {
        var properties = new AuthenticationProperties 
        { 
            RedirectUri = Url.Action("GoogleResponse") // Куда вернуться после успеха
        };
    
        // Этот метод отправит пользователя на сервер Google
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /*
    [HttpPost("auth/google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
        if (!result.Succeeded)
            return BadRequest("Ошибка авторизации Google");

        AuthAccountDTO userData = new AuthAccountDTO
        {
            Name = result.Principal.Identity.Name,
            Email = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            TelegramId = null,
        };
        
        var command = new AuthAccountCommand(userData);

        BaseResponse<IdentityDTO> response = await _mediator.Send(command);
        if (response.Status != ResponseStatus.Success)
        {
            return BadRequest(response.ErrorMessage);
        }
        
        UserClaimsModel model = new UserClaimsModel(response.Data);
        var token = _jwtService.GenerateToken(model);
        var frontendUrl = $"{_configuration["Frontend"]}/auth-callback";
        return Redirect($"{frontendUrl}?token={token}");
    }
    */
    [HttpGet("auth/google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return BadRequest("Ошибка авторизации Google");

        var userData = new AuthAccountDTO
        {
            Name = result.Principal.Identity.Name,
            Email = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            TelegramId = null,
        };

        var response = await _mediator.Send(new AuthAccountCommand(userData));
        if (response.Status != ResponseStatus.Success)
            return BadRequest(response.ErrorMessage);

        var token = _jwtService.GenerateToken(new UserClaimsModel(response.Data));
        return Redirect($"{_configuration["Frontend"]}/auth-callback?token={token}");
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout()
    {
        HttpContext.Response.Cookies.Delete("jwt_token");
        return Ok();
    }
    
    
    [Authorize] // Этот атрибут проверяет наличие и валидность JWT
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var response = await _mediator.Send(new GetProfileQuery(UserId));
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] int pageSize, [FromQuery] int page)
    {
        var response = await _mediator.Send(new GetUserOrdersQuery(UserId, pageSize, page));

        return Ok(response);
    }

    [Authorize]
    [HttpGet("product-status/{id}")]
    public async Task<IActionResult> GetCustomerProductStatus([FromRoute] int id)
    {
        var response = await _mediator.Send(new GetCustomerProductStatusQuery(UserId, id));
        return Ok(response);
    }

    /// <summary>
    /// Все купленные файлы текущего пользователя, сгруппированные по продуктам.
    /// </summary>
    [Authorize]
    [HttpGet("my-files")]
    public async Task<IActionResult> GetMyFiles()
    {
        var response = await _mediator.Send(new GetMyFilesQuery(UserId));
        return Ok(response);
    }

    /// <summary>
    /// Получить presigned URL для скачивания купленного файла (действует 10 минут).
    /// </summary>
    [Authorize]
    [HttpGet("download/{contentFileId}")]
    public async Task<IActionResult> DownloadContentFile([FromRoute] Guid contentFileId)
    {
        var response = await _mediator.Send(new GetDownloadLinkQuery(UserId, contentFileId));
        return Ok(response);
    }

    /// <summary>
    /// История транзакций текущего пользователя (пополнения, покупки, выводы).
    /// </summary>
    [Authorize]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50)
    {
        var response = await _mediator.Send(new GetUserTransactionsQuery(UserId, page, pageSize));
        return Ok(response);
    }
}
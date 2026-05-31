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
using Microsoft.AspNetCore.RateLimiting;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")] 
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;
    
    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    
    public AccountController(IMediator mediator,  JwtService jwtService, IConfiguration configuration)
    {
        _mediator=mediator;
        _jwtService=jwtService;
        _configuration=configuration;
    }
    
    [EnableRateLimiting("auth")]
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
            return Redirect($"{_configuration["Frontend"]}/signin?error=google_failed");

        var email = result.Principal!.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"{_configuration["Frontend"]}/signin?error=no_email");

        // Режим привязки Google к существующему аккаунту (Telegram-пользователь)
        result.Properties!.Items.TryGetValue("link_user_id", out var linkUserIdStr);
        if (!string.IsNullOrEmpty(linkUserIdStr) && Guid.TryParse(linkUserIdStr, out var linkUserId))
        {
            var linkResponse = await _mediator.Send(new LinkEmailAccountCommand(linkUserId, email));
            if (linkResponse.Status != ResponseStatus.Success)
                return Redirect($"{_configuration["Frontend"]}/me/profile?link_error={Uri.EscapeDataString(linkResponse.ErrorMessage ?? "Ошибка привязки")}");

            // Обновляем JWT с новыми данными (email теперь есть)
            var token = _jwtService.GenerateToken(new UserClaimsModel(linkResponse.Data));
            return Redirect($"{_configuration["Frontend"]}/me/profile?linked=google&token={token}");
        }

        // Обычный вход / регистрация через Google
        var userData = new AuthAccountDTO
        {
            Name       = result.Principal.Identity!.Name ?? email,
            Email      = email,
            TelegramId = null,
        };

        var response = await _mediator.Send(new AuthAccountCommand(userData));
        if (response.Status != ResponseStatus.Success)
            return Redirect($"{_configuration["Frontend"]}/signin?error={Uri.EscapeDataString(response.ErrorMessage ?? "auth_failed")}");

        var jwtToken = _jwtService.GenerateToken(new UserClaimsModel(response.Data));
        return Redirect($"{_configuration["Frontend"]}/auth-callback?token={jwtToken}");
    }

    /// <summary>
    /// Инициирует привязку Google-аккаунта к Telegram-пользователю.
    /// [Authorize] — userId берётся из JWT. После OAuth редиректит обратно в GoogleResponse.
    /// </summary>
    [Authorize]
    [HttpGet("link-google")]
    public IActionResult LinkGoogle()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleResponse"),
            Items       = { ["link_user_id"] = UserId.ToString() },
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Привязывает Telegram к аккаунту пользователя (для тех, кто зашёл через Google).
    /// Данные берутся из Telegram Login Widget.
    /// </summary>
    [Authorize]
    [HttpPost("link-telegram")]
    public async Task<IActionResult> LinkTelegram([FromBody] TelegramAuthData data)
    {
        var response = await _mediator.Send(new LinkTelegramAccountCommand(UserId, data));
        return Ok(response);
    }

    /// <summary>
    /// Авторизация / регистрация через Telegram Login Widget.
    /// Возвращает JWT токен.
    /// </summary>
    [EnableRateLimiting("auth")]
    [HttpPost("auth/telegram")]
    public async Task<IActionResult> TelegramAuth([FromBody] TelegramAuthData data)
    {
        var response = await _mediator.Send(new AuthTelegramAccountCommand(data));
        if (response.Status != ResponseStatus.Success)
            return Ok(response);

        var token = _jwtService.GenerateToken(new UserClaimsModel(response.Data));
        return Ok(BaseResponse<string>.Success(token));
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

    /// <summary>
    /// Обновить настройки уведомлений текущего пользователя.
    /// </summary>
    [Authorize]
    [HttpPut("notification-settings")]
    public async Task<IActionResult> UpdateNotificationSettings(
        [FromBody] UpdateNotificationSettingsDto dto)
    {
        var response = await _mediator.Send(
            new UpdateNotificationSettingsCommand(UserId, dto.NotifyOnPurchase, dto.NotifyOnModeration));
        return Ok(response);
    }
}

public record UpdateNotificationSettingsDto(bool NotifyOnPurchase, bool NotifyOnModeration);
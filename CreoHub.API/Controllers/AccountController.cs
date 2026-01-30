using System.Net;
using System.Security.Claims;
using CreoHub.API.Models;
using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Queries.Account;
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
    [HttpPost("auth/google-response")]
    public async Task<IActionResult> GoogleResponse([FromForm] string credential)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(credential);

        var userData = new AuthAccountDTO
        {
            Name = payload.Name,
            Email = payload.Email,
            TelegramId = null
        };

        var response = await _mediator.Send(new AuthAccountCommand(userData, DateTime.MinValue));
        if (response.Status != ResponseStatus.Success)
            return BadRequest(response.ErrorMessage);

        var token = _jwtService.GenerateToken(new UserClaimsModel(response.Data));
        HttpContext.Response.Cookies.Append("jwt_token", token, new CookieOptions
        {
            HttpOnly = true,         
            Secure = false,          
            SameSite = SameSiteMode.Lax, 
            Path = "/",              
            Expires = DateTime.UtcNow.AddDays(7)
        });
        return Redirect($"{_configuration["Frontend"]}/");
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
        Guid id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var response = await _mediator.Send(new GetProfileQuery(id));
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response);
        }
        
        return Ok(response);
    }
    
    
}
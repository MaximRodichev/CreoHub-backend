using System.IO.Compression;
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
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private readonly IOrderRepository _orderRepository;
    private readonly IStorageService _storageService;

    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    public AccountController(
        IMediator mediator,
        JwtService jwtService,
        IConfiguration configuration,
        IOrderRepository orderRepository,
        IStorageService storageService)
    {
        _mediator         = mediator;
        _jwtService       = jwtService;
        _configuration    = configuration;
        _orderRepository  = orderRepository;
        _storageService   = storageService;
    }

    /// <summary>
    /// Завершает OAuth-редирект на фронт. Если задан CookieDomain (общий для api.creohub.xyz
    /// и creohub.xyz) — ставит JWT в HttpOnly cookie и НЕ кладёт токен в URL (не светится в
    /// логах/истории/referer). Если домен не задан (dev / single-host) — прежний fallback ?token=,
    /// чтобы логин не сломался без настройки env.
    /// </summary>
    private IActionResult AuthCallbackRedirect(string jwt, string path)
    {
        var frontend = _configuration["Frontend"];
        var domain   = _configuration["CookieDomain"];

        if (!string.IsNullOrWhiteSpace(domain))
        {
            Response.Cookies.Append("jwt_token", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Path     = "/",
                MaxAge   = TimeSpan.FromDays(7),
                Domain   = domain,
            });
            return Redirect($"{frontend}{path}");
        }

        var sep = path.Contains('?') ? "&" : "?";
        return Redirect($"{frontend}{path}{sep}token={jwt}");
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

            // Обновляем JWT с новыми данными (email теперь есть) — в cookie, не в URL
            var token = _jwtService.GenerateToken(new UserClaimsModel(linkResponse.Data));
            return AuthCallbackRedirect(token, "/me/profile?linked=google");
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
        return AuthCallbackRedirect(jwtToken, "/auth-callback");
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

    /// <summary>
    /// GET-версия Telegram авторизации — используется виджетом на мобиле (data-auth-url).
    /// Telegram редиректит сюда с параметрами в query string после подтверждения в приложении.
    /// Устанавливает cookie и редиректит на фронтенд.
    /// </summary>
    [EnableRateLimiting("auth")]
    [HttpGet("auth/telegram/callback")]
    public async Task<IActionResult> TelegramAuthCallback(
        [FromQuery] long    id,
        [FromQuery] string  first_name,
        [FromQuery] long    auth_date,
        [FromQuery] string  hash,
        [FromQuery] string? last_name  = null,
        [FromQuery] string? username   = null,
        [FromQuery] string? photo_url  = null)
    {
        var data = new TelegramAuthData(id, first_name, auth_date, hash, last_name, username, photo_url);
        var response = await _mediator.Send(new AuthTelegramAccountCommand(data));
        if (response.Status != ResponseStatus.Success)
            return Redirect($"{_configuration["Frontend"]}/signin?tg_error={Uri.EscapeDataString(response.ErrorMessage ?? "auth_failed")}");

        var token = _jwtService.GenerateToken(new UserClaimsModel(response.Data));
        return AuthCallbackRedirect(token, "/auth-callback");
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
    /// Стримит все файлы заказа в ZIP-архив. Файлы записываются без повторного сжатия
    /// (большинство уже сжаты: .7z, .mp4 и т.д.), что ускоряет генерацию.
    /// </summary>
    [Authorize]
    [HttpGet("order/{orderId}/download-all")]
    public async Task<IActionResult> DownloadOrderAll([FromRoute] Guid orderId, CancellationToken ct)
    {
        var files = await _orderRepository.GetOrderDownloadFilesAsync(orderId, UserId);
        if (files is null)
            return NotFound("Order not found or not paid.");
        if (files.Count == 0)
            return NotFound("No files in this order.");

        var safeOrderId   = orderId.ToString()[..8].ToUpper();
        var productNames  = await _orderRepository.GetOrderProductNamesAsync(orderId, UserId) ?? [];
        var zipName       = ZipFileNameHelper.BuildZipFileName(productNames, safeOrderId);

        // ZipArchive.Dispose() пишет центральный каталог синхронно.
        // Kestrel запрещает синхронные записи по умолчанию — разрешаем только для этого запроса.
        var syncIo = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
        if (syncIo != null) syncIo.AllowSynchronousIO = true;

        Response.StatusCode  = 200;
        Response.ContentType = "application/zip";
        // RFC 5987: filename= (ASCII-заглушка) + filename*= (UTF-8, percent-encoded)
        // Это единственный корректный способ передать не-ASCII имена в HTTP-заголовке.
        var asciiName   = System.Text.RegularExpressions.Regex.Replace(zipName, @"[^\x20-\x7E]", "_");
        var encodedName = Uri.EscapeDataString(zipName);   // "Full%20Pack%20%D0%9C%D0%B0%D0%BA%D1%81.zip"
        Response.Headers.Append("Content-Disposition",
            $"attachment; filename=\"{asciiName}\"; filename*=UTF-8''{encodedName}");

        // Отключаем буферизацию — стримим байты напрямую клиенту
        Response.Headers.Append("X-Accel-Buffering", "no");

        using var archive = new ZipArchive(Response.Body, ZipArchiveMode.Create, leaveOpen: true);

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, fileName) in files)
        {
            if (ct.IsCancellationRequested) break;

            // Гарантируем уникальность имён внутри архива
            var entryName = fileName;
            var counter   = 1;
            while (!usedNames.Add(entryName))
                entryName = $"{Path.GetFileNameWithoutExtension(fileName)}_{counter++}{Path.GetExtension(fileName)}";

            var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
            await using var entryStream = entry.Open();
            await using var r2Stream    = await _storageService.OpenReadStreamAsync(key);
            await r2Stream.CopyToAsync(entryStream, ct);
        }

        return new EmptyResult();
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
        var response = await _mediator.Send(new UpdateNotificationSettingsCommand(
            UserId,
            dto.TelegramEnabled,
            dto.EmailEnabled,
            dto.NotifyOnPurchase,
            dto.NotifyOnModeration,
            dto.NotifyOnBalance,
            dto.NotifyOnBroadcast,
            dto.NotifyOnNewProduct));
        return Ok(response);
    }

    // ── In-app notifications ──────────────────────────────────────────────────

    /// <summary>
    /// Список непрочитанных in-app уведомлений (max 20).
    /// </summary>
    [Authorize]
    [HttpGet("in-app-notifications")]
    public async Task<IActionResult> GetInAppNotifications(
        [FromServices] IInAppNotificationRepository repo)
    {
        var items = await repo.GetPendingAsync(UserId);
        return Ok(BaseResponse<object>.Success(items.Select(n => new
        {
            id        = n.Id,
            type      = n.Type.ToString(),
            message   = n.Message,
            actionUrl = n.ActionUrl,
            createdAt = n.CreatedAt,
        })));
    }

    /// <summary>
    /// Полная история уведомлений (прочитанные + непрочитанные), опц. фильтр по типу.
    /// </summary>
    [Authorize]
    [HttpGet("in-app-notifications/history")]
    public async Task<IActionResult> GetNotificationHistory(
        [FromServices] IInAppNotificationRepository repo,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        NotificationType? parsed =
            Enum.TryParse<NotificationType>(type, ignoreCase: true, out var t) ? t : null;

        var p  = page < 1 ? 1 : page;
        var ps = pageSize is < 1 or > 100 ? 50 : pageSize;

        var items = await repo.GetHistoryAsync(UserId, parsed, p, ps);
        return Ok(BaseResponse<object>.Success(items.Select(n => new
        {
            id        = n.Id,
            type      = n.Type.ToString(),
            message   = n.Message,
            actionUrl = n.ActionUrl,
            createdAt = n.CreatedAt,
            isRead    = n.IsRead,
            readAt    = n.ReadAt,
        })));
    }

    /// <summary>
    /// Отметить одно уведомление как прочитанное.
    /// </summary>
    [Authorize]
    [HttpPost("in-app-notifications/{id:int}/ack")]
    public async Task<IActionResult> AcknowledgeNotification(
        [FromRoute] int id,
        [FromServices] IInAppNotificationRepository repo)
    {
        var ok = await repo.AcknowledgeAsync(id, UserId);
        return Ok(ok ? BaseResponse<bool>.Success(true) : BaseResponse<bool>.Fail("Not found"));
    }

    /// <summary>
    /// Отметить все уведомления как прочитанные.
    /// </summary>
    [Authorize]
    [HttpPost("in-app-notifications/ack-all")]
    public async Task<IActionResult> AcknowledgeAllNotifications(
        [FromServices] IInAppNotificationRepository repo)
    {
        await repo.AcknowledgeAllAsync(UserId);
        return Ok(BaseResponse<bool>.Success(true));
    }
}

public record UpdateNotificationSettingsDto(
    bool TelegramEnabled,
    bool EmailEnabled,
    bool NotifyOnPurchase,
    bool NotifyOnModeration,
    bool NotifyOnBalance,
    bool NotifyOnBroadcast,
    bool NotifyOnNewProduct);

file static class ZipFileNameHelper
{
    private static readonly char[] InvalidChars =
        Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|', '#']).Distinct().ToArray();

    /// <summary>
    /// Формирует безопасное имя ZIP-файла вида:
    ///   1 товар:  "ProductName-order-ABC12345.zip"
    ///   2 товара: "Product1+Product2-order-ABC12345.zip"
    ///   3+:       "Product1+2more-order-ABC12345.zip"
    /// Общая длина ограничена 120 символами (без .zip).
    /// </summary>
    public static string BuildZipFileName(IReadOnlyList<string> productNames, string shortId)
    {
        if (productNames.Count == 0)
            return $"order-{shortId}.zip";

        var label = productNames.Count switch
        {
            1 => Sanitize(productNames[0]),
            2 => $"{Sanitize(productNames[0])}+{Sanitize(productNames[1])}",
            _ => $"{Sanitize(productNames[0])}+{productNames.Count - 1}more"
        };

        // Общая длина: label + "-order-" + shortId = label + 15 символов
        const int maxLabel = 120 - 15;
        if (label.Length > maxLabel)
            label = label[..maxLabel].TrimEnd('-', '_', '+');

        return $"{label}-order-{shortId}.zip";
    }

    private static string Sanitize(string name)
    {
        // Заменяем пробелы на подчёркивания, убираем запрещённые символы
        var clean = string.Concat(
            name.Replace(' ', '_')
                .Where(c => Array.IndexOf(InvalidChars, c) < 0));

        return string.IsNullOrWhiteSpace(clean) ? "Product" : clean;
    }
}
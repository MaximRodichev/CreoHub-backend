using System.Security.Claims;
using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Admin;
using CreoHub.Application.Queries.AnalyticsQueries;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Queries.Shop;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

// DTOs for admin product actions
public record AdminBanProductDto(string Reason);
public record AdminRejectProductDto(string? Reason = null);
public record AdminHideProductDto(string? Reason = null);
public record AdminSendToModerationDto(string? Reason = null);

// DTOs for broadcast
public record CreateBroadcastDto(string Message);

// DTO for content-replacement rejection
public record AdminRejectReplacementDto(string? Reason = null);

// DTO for account merge
public record AdminMergeAccountsDto(Guid KeepId, Guid MergeId);

// DTO for manager-created shop
public record AdminCreateShopDto(Guid OwnerUserId, string Name, string Description);

/// <summary>
/// Все эндпоинты доступны только пользователям с ролью Admin.
/// Авторизация через политику "Admin" (ClaimTypes.Role == "Admin").
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    private Guid GetAdminUserId()
    {
        var sub = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    // ══════════════════════════════════════════
    // ПОЛЬЗОВАТЕЛИ
    // ══════════════════════════════════════════

    /// <summary>Список всех пользователей с базовой статистикой.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
var result = await _mediator.Send(new GetAdminUsersQuery());
        return Ok(result);
    }

    /// <summary>Полный профиль пользователя + последние 50 заказов.</summary>
    [HttpGet("user/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
var result = await _mediator.Send(new GetAdminUserDetailQuery(id));
        return Ok(result);
    }

    /// <summary>Создать клиента вручную (без регистрации через OAuth).</summary>
    [HttpPost("user")]
    public async Task<IActionResult> CreateClient([FromBody] AdminCreateClientDto dto)
    {
var result = await _mediator.Send(new CreateClientCommand(dto.Name, dto.TelegramId, dto.TelegramUsername, dto.Email));
        return result.Status == Application.DTO.ResponseStatus.Success
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>Предпросмотр объединения аккаунтов (что переедет + результат гардов). Ничего не меняет.</summary>
    [HttpGet("users/merge-preview")]
    public async Task<IActionResult> MergePreview([FromQuery] Guid keepId, [FromQuery] Guid mergeId)
        => Ok(await _mediator.Send(new GetMergePreviewQuery(keepId, mergeId)));

    /// <summary>Объединить аккаунты: перенести данные mergeId → keepId, затем удалить mergeId. Необратимо.</summary>
    [HttpPost("users/merge")]
    public async Task<IActionResult> MergeAccounts([FromBody] AdminMergeAccountsDto dto)
    {
        var result = await _mediator.Send(new MergeAccountsCommand(dto.KeepId, dto.MergeId, GetAdminUserId()));
        return result.Status == Application.DTO.ResponseStatus.Success
            ? Ok(result)
            : BadRequest(result);
    }

    // ══════════════════════════════════════════
    // ПОВЕДЕНИЕ / СПРОС
    // ══════════════════════════════════════════

    /// <summary>История поиска: пагинация + фильтр noResults=true (бэклог спроса).</summary>
    [HttpGet("searches")]
    public async Task<IActionResult> GetSearches(
        [FromQuery] int days = 30, [FromQuery] bool noResults = false,
        [FromQuery] int page = 0, [FromQuery] int pageSize = 50)
        => Ok(await _mediator.Send(new GetSearchHistoryQuery(days, noResults, page, pageSize)));

    /// <summary>Путь субъекта (по userId ИЛИ sessionId) — хронология событий.</summary>
    [HttpGet("flow")]
    public async Task<IActionResult> GetFlow(
        [FromQuery] Guid? userId, [FromQuery] string? sessionId, [FromQuery] int days = 30)
        => Ok(await _mediator.Send(new GetSubjectFlowQuery(userId, sessionId, days)));

    // ══════════════════════════════════════════
    // МАГАЗИНЫ
    // ══════════════════════════════════════════

    /// <summary>Все магазины с выручкой и количеством продуктов.</summary>
    [HttpGet("shops")]
    public async Task<IActionResult> GetShops()
    {
var result = await _mediator.Send(new GetAdminShopsQuery());
        return Ok(result);
    }

    /// <summary>Менеджер создаёт магазин для пользователя (само-создание отключено).</summary>
    [HttpPost("shop")]
    public async Task<IActionResult> CreateShop([FromBody] AdminCreateShopDto dto)
    {
        var result = await _mediator.Send(new AdminCreateShopCommand(dto.OwnerUserId, dto.Name, dto.Description, GetAdminUserId()));
        return result.Status == Application.DTO.ResponseStatus.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Дашборд конкретного магазина (те же данные что видит владелец).</summary>
    [HttpGet("shop/{shopId:guid}/dashboard")]
    public async Task<IActionResult> GetShopDashboard(
        Guid shopId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
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
var result = await _mediator.Send(new GetOrdersShortInfoByShopIdQuery(shopId, from, to));
        return Ok(result);
    }

    /// <summary>Клиенты (покупатели) конкретного магазина.</summary>
    [HttpGet("shop/{shopId:guid}/clients")]
    public async Task<IActionResult> GetShopClients(Guid shopId)
    {
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
var result = await _mediator.Send(new GetAdminProductsListQuery());
        return Ok(result);
    }

    // ══════════════════════════════════════════
    // ТОВАРЫ — статусы
    // ══════════════════════════════════════════

    /// <summary>Забанить товар с причиной.</summary>
    [HttpPost("product/{id:int}/ban")]
    public async Task<IActionResult> BanProduct(int id, [FromBody] AdminBanProductDto dto)
    {
var adminId = GetAdminUserId();
        var result  = await _mediator.Send(new BanProductCommand(id, dto.Reason, adminId));
        return Ok(result);
    }

    /// <summary>Снять бан с товара.</summary>
    [HttpPost("product/{id:int}/unban")]
    public async Task<IActionResult> UnbanProduct(int id)
    {
var adminId = GetAdminUserId();
        var result  = await _mediator.Send(new UnbanProductCommand(id, adminId));
        return Ok(result);
    }

    /// <summary>История изменений статуса товара.</summary>
    [HttpGet("product/{id:int}/status-log")]
    public async Task<IActionResult> GetProductStatusLog(int id)
    {
var result = await _mediator.Send(new GetProductStatusLogQuery(id));
        return Ok(result);
    }

    /// <summary>Список товаров по статусу для панели модерации. status: OnModerating | Banned | Hidden</summary>
    [HttpGet("moderation")]
    public async Task<IActionResult> GetModerationQueue([FromQuery] string status = "OnModerating")
    {
var result = await _mediator.Send(new GetProductsOnModerationQuery(status));
        return Ok(result);
    }

    /// <summary>Полная информация о товаре (для просмотра в очереди модерации). Работает при любом статусе.</summary>
    [HttpGet("product/{id:int}")]
    public async Task<IActionResult> GetProductDetail(int id)
    {
var result = await _mediator.Send(new GetProductInfoByIdQuery(id));
        return Ok(result);
    }

    /// <summary>История правок товара (JSON-снапшоты состояния до каждого сохранения).</summary>
    [HttpGet("product/{id:int}/edit-history")]
    public async Task<IActionResult> GetProductEditHistory(int id)
    {
var result = await _mediator.Send(new GetProductEditHistoryQuery(id));
        return Ok(result);
    }

    /// <summary>Контент-файлы товара + presigned-ссылки на скачивание — проверить, что внутри (модерация).</summary>
    [HttpGet("product/{id:int}/content-files")]
    public async Task<IActionResult> GetProductContentFiles(int id)
    {
        var result = await _mediator.Send(new GetAdminProductContentFilesQuery(id));
        return Ok(result);
    }

    // ══════════════════════════════════════════
    // ЗАМЕНА КОНТЕНТ-ФАЙЛОВ (модерация)
    // ══════════════════════════════════════════

    /// <summary>Очередь заявок на замену контент-файла (старый + новый файл со ссылками для сравнения).</summary>
    [HttpGet("content-replacements")]
    public async Task<IActionResult> GetContentReplacements()
        => Ok(await _mediator.Send(new GetPendingContentReplacementsQuery()));

    /// <summary>Одобрить замену: подменяет байты исходного файла (покупатели получают новый).</summary>
    [HttpPost("content-replacement/{id:guid}/approve")]
    public async Task<IActionResult> ApproveContentReplacement(Guid id)
        => Ok(await _mediator.Send(new ApproveContentReplacementCommand(id, GetAdminUserId())));

    /// <summary>Отклонить замену: удаляет загруженный файл, уведомляет владельца.</summary>
    [HttpPost("content-replacement/{id:guid}/reject")]
    public async Task<IActionResult> RejectContentReplacement(Guid id, [FromBody] AdminRejectReplacementDto dto)
        => Ok(await _mediator.Send(new RejectContentReplacementCommand(id, GetAdminUserId(), dto.Reason)));

    // ══════════════════════════════════════════
    // OG-КАРТОЧКИ
    // ══════════════════════════════════════════

    /// <summary>Перегенерировать og:image карточки ВСЕХ активных товаров (фоном, троттлинг).</summary>
    [HttpPost("og/regenerate")]
    public async Task<IActionResult> RegenerateAllOg()
        => Ok(await _mediator.Send(new RegenerateAllProductOgCommand()));

    /// <summary>Принудительно скрыть любой товар.</summary>
    [HttpPost("product/{id:int}/hide")]
    public async Task<IActionResult> HideProduct(int id, [FromBody] AdminHideProductDto dto)
    {
var result = await _mediator.Send(new AdminHideProductCommand(id, GetAdminUserId(), dto.Reason));
        return Ok(result);
    }

    /// <summary>Принудительно отправить любой товар на модерацию.</summary>
    [HttpPost("product/{id:int}/send-to-moderation")]
    public async Task<IActionResult> AdminSendToModeration(int id, [FromBody] AdminSendToModerationDto dto)
    {
var result = await _mediator.Send(new AdminSendToModerationCommand(id, GetAdminUserId(), dto.Reason));
        return Ok(result);
    }

    /// <summary>Одобрить модерацию товара.</summary>
    [HttpPost("product/{id:int}/approve")]
    public async Task<IActionResult> ApproveProduct(int id)
    {
var result = await _mediator.Send(new ApproveModerationCommand(id, GetAdminUserId()));
        return Ok(result);
    }

    /// <summary>Отклонить модерацию товара.</summary>
    [HttpPost("product/{id:int}/reject")]
    public async Task<IActionResult> RejectProduct(int id, [FromBody] AdminRejectProductDto dto)
    {
var result = await _mediator.Send(new RejectModerationCommand(id, GetAdminUserId(), dto.Reason));
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
var result = await _mediator.Send(new CreateOrderDevCommand(dto));
        return result.Status == Application.DTO.ResponseStatus.Success
            ? Ok(result)
            : BadRequest(result);
    }

    // ══════════════════════════════════════════
    // АНАЛИТИКА
    // ══════════════════════════════════════════

    /// <summary>Platform-wide event metrics: search queries, views, purchases, etc.</summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics([FromQuery] int days = 30)
    {
var result = await _mediator.Send(new GetAdminAnalyticsQuery(days));
        return Ok(result);
    }

    /// <summary>Дашборд активности: новые юзеры, события по дням, лента активности, топ-поиски/страницы.</summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] int days = 14)
    {
var result = await _mediator.Send(new GetAdminActivityQuery(days));
        return Ok(result);
    }

    // ══════════════════════════════════════════
    // РАССЫЛКИ
    // ══════════════════════════════════════════

    /// <summary>Создать задание на рассылку всем пользователям (message через HTML разметку).</summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> CreateBroadcast([FromBody] CreateBroadcastDto dto)
    {
var result = await _mediator.Send(new CreateBroadcastJobCommand(dto.Message, GetAdminUserId()));
        return result.Status == Application.DTO.ResponseStatus.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Список всех заданий рассылки (история + текущий статус).</summary>
    [HttpGet("broadcasts")]
    public async Task<IActionResult> GetBroadcasts()
    {
var result = await _mediator.Send(new GetBroadcastJobsQuery());
        return Ok(result);
    }
}

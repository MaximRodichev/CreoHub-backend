using CreoHub.API.Configuration;
using CreoHub.Application.Commands.StorageCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Queries.Storage;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("[controller]")]
public class S3Controller : ShopOwnerControllerBase
{
    private readonly IMediator                      _mediator;
    private readonly IShopRepository                _shopRepository;
    private readonly IOptimizationProgressService   _progressService;

    public S3Controller(
        IMediator mediator,
        IShopRepository shopRepository,
        IOptimizationProgressService progressService)
    {
        _mediator        = mediator;
        _shopRepository  = shopRepository;
        _progressService = progressService;
    }

    // ── Presigned upload (new flow) ───────────────────────────────────────────

    public record RequestUploadDto(string FileName, string MimeType, long FileSize);
    public record ConfirmUploadDto(string Key, string FileName, string MimeType, long FileSize);

    /// <summary>
    /// Шаг 1: получить presigned PUT URL для прямой загрузки в R2.
    /// БД не трогается — только генерируется ключ и URL.
    /// </summary>
    [Authorize]
    [HttpPost("request-upload")]
    public async Task<IActionResult> RequestUpload([FromBody] RequestUploadDto dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var limit = FileLimits.For(dto.MimeType);
        if (dto.FileSize > limit)
            return Ok(BaseResponse<bool>.Fail($"Файл слишком большой. Максимум для {dto.MimeType}: {FileLimits.Format(limit)}"));

        var command  = new RequestStorageUploadCommand(dto.FileName, dto.MimeType, dto.FileSize, shopId, limit);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Шаг 2: подтвердить загрузку. Backend делает HEAD к R2, создаёт StorageObject в БД.
    /// Вызывается только после успешного PUT напрямую в R2.
    /// </summary>
    [Authorize]
    [HttpPost("confirm-upload")]
    public async Task<IActionResult> ConfirmUpload([FromBody] ConfirmUploadDto dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new ConfirmStorageUploadCommand(dto.Key, dto.FileName, dto.MimeType, dto.FileSize, shopId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Upload (legacy — оставлен для совместимости) ──────────────────────────

    [Authorize]
    [RequestSizeLimit(FileLimits.MaxUpload)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileLimits.MaxUpload)]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        using var stream = file.OpenReadStream();
        var fileSize = stream.Length;
        var limit    = FileLimits.For(file.ContentType);

        if (fileSize > limit)
            return Ok(BaseResponse<bool>.Fail($"Файл слишком большой. Максимум для {file.ContentType}: {FileLimits.Format(limit)}"));

        var command  = new UploadStorageObjectCommand(stream, file.FileName, file.ContentType, shopId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Authorize]
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new DeleteFile(id, shopId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Files list ────────────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] string? type     = null,
        [FromQuery] string  sort     = "date",
        [FromQuery] string  order    = "desc")
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var query    = new GetStorageObjectsQuery(shopId, page, pageSize, search, type, sort, order);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    // ── Media ─────────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("attachMedia")]
    public async Task<IActionResult> AttachMedia([FromBody] AttachMediaDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new AttachMedia(shopId, dto.ProductId, dto.StorageObjectId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("detachMedia")]
    public async Task<IActionResult> DetachMedia([FromBody] DetachMediaDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new DetachMedia(shopId, dto.StorageObjectId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Content ───────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("attachContent")]
    public async Task<IActionResult> AttachContent([FromBody] AttachContentFileDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new AttachContentFileCommand(shopId, dto);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("detachContent")]
    public async Task<IActionResult> DetachContent([FromBody] DetachContentFileDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new DetachContentFileCommand(shopId, dto);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Content replacement (через модерацию) ─────────────────────────────────

    public record RequestContentReplacementDto(Guid ContentFileId, Guid NewStorageObjectId);

    /// <summary>Продавец просит заменить байты контент-файла новым (уже загруженным) файлом — на модерацию.</summary>
    [Authorize]
    [HttpPost("content-replacement")]
    public async Task<IActionResult> RequestContentReplacement([FromBody] RequestContentReplacementDto dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(
            new RequestContentReplacementCommand(shopId, dto.ContentFileId, dto.NewStorageObjectId));
        return Ok(response);
    }

    // ── Sort order ────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPatch("media/sort-order")]
    public async Task<IActionResult> UpdateMediaSortOrder([FromBody] UpdateMediaSortOrderDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(
            new UpdateMediaSortOrderCommand(shopId, dto.ProductId, dto.StorageObjectId, dto.SortOrder));
        return Ok(response);
    }

    // ── Content file update ───────────────────────────────────────────────────

    [Authorize]
    [HttpPatch("content/{contentFileId}")]
    public async Task<IActionResult> UpdateContentFile(
        [FromRoute] Guid contentFileId,
        [FromBody]  UpdateContentFileDTO dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(
            new UpdateContentFileCommand(shopId, contentFileId, dto));
        return Ok(response);
    }

    // ── Optimize ──────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("optimize/{storageObjectId}")]
    public async Task<IActionResult> Optimize(Guid storageObjectId)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var command  = new OptimizeStorageObjectCommand(storageObjectId, shopId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    // ── Owner download (presigned URL with Content-Disposition) ──────────────

    /// <summary>Одиночный файл: presigned URL с правильным именем для скачивания.</summary>
    [Authorize]
    [HttpGet("owner-download")]
    public async Task<IActionResult> OwnerDownload([FromQuery] Guid id)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var query    = new OwnerDownloadQuery(id, shopId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    public record BulkDownloadRequestDto(List<Guid> Ids);

    /// <summary>Пакетный запрос: список presigned URL для скачивания через aria2c/wget.</summary>
    [Authorize]
    [HttpPost("bulk-download-urls")]
    public async Task<IActionResult> BulkDownloadUrls([FromBody] BulkDownloadRequestDto dto)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var query    = new BulkOwnerDownloadQuery(dto.Ids, shopId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    // ── Content detach info ───────────────────────────────────────────────────

    /// <summary>
    /// Проверяет можно ли чисто отвязать контент файл (нет ContentAccess)
    /// или нужно архивирование (есть покупатели).
    /// </summary>
    [Authorize]
    [HttpGet("content-detach-info")]
    public async Task<IActionResult> ContentDetachInfo(
        [FromQuery] int  productId,
        [FromQuery] Guid storageObjectId)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var query    = new ContentDetachInfoQuery(productId, storageObjectId, shopId);
        var response = await _mediator.Send(query);
        return Ok(response);
    }

    // ── Optimization progress ─────────────────────────────────────────────────

    /// <summary>
    /// Возвращает прогресс FFmpeg-оптимизации для конкретного файла.
    /// 0 = в очереди / не начат, 1-95 = конвертация H.264,
    /// 96 = генерация thumbnail, 100 = готово, -1 = ошибка.
    /// </summary>
    [Authorize]
    [HttpGet("optimization-progress/{storageObjectId:guid}")]
    public IActionResult GetOptimizationProgress(Guid storageObjectId)
    {
        var progress = _progressService.GetProgress(storageObjectId);
        return Ok(BaseResponse<int>.Success(progress));
    }

    // ── Backfill thumbnails ───────────────────────────────────────────────────

    [Authorize]
    [HttpPost("backfill-thumbnails")]
    public async Task<IActionResult> BackfillThumbnails()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var response = await _mediator.Send(new BackfillThumbnailsCommand(shopId));
        return Ok(response);
    }
}

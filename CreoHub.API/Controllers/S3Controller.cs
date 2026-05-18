using CreoHub.Application.Commands.StorageCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Queries.Storage;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("[controller]")]
public class S3Controller : ShopOwnerControllerBase
{
    private static readonly Dictionary<string, long> Limits = new()
    {
        { "video",       50L  * 1024 * 1024 },
        { "image",       5L   * 1024 * 1024 },
        { "application", 2L   * 1024 * 1024 * 1024 },
    };

    public static long GetLimit(string mimeType)
    {
        var category = mimeType.Split('/')[0];
        return Limits.TryGetValue(category, out var limit) ? limit : 10L * 1024 * 1024;
    }

    private readonly IMediator        _mediator;
    private readonly IShopRepository  _shopRepository;

    public S3Controller(IMediator mediator, IShopRepository shopRepository)
    {
        _mediator       = mediator;
        _shopRepository = shopRepository;
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

        var limit = GetLimit(dto.MimeType);
        if (dto.FileSize > limit)
        {
            var limitMb = limit / 1024 / 1024;
            return Ok(BaseResponse<bool>.Fail($"Файл слишком большой. Максимум для {dto.MimeType}: {limitMb} MB"));
        }

        var command  = new RequestStorageUploadCommand(dto.FileName, dto.MimeType, dto.FileSize, shopId);
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
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024)]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        using var stream = file.OpenReadStream();
        var fileSize = stream.Length;
        var limit    = GetLimit(file.ContentType);

        if (fileSize > limit)
        {
            var limitMb = limit / 1024 / 1024;
            return Ok(BaseResponse<bool>.Fail($"Файл слишком большой. Максимум для {file.ContentType}: {limitMb} MB"));
        }

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
    public async Task<IActionResult> GetFiles()
    {
        var (ok, shopId) = await TryGetShopId(_shopRepository);
        if (!ok) return StatusCode(403, BaseResponse<bool>.Fail("У вас нет магазина"));

        var query    = new GetStorageObjectsQuery(shopId);
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

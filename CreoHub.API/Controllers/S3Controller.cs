
using System.Security.Claims;
using Amazon.S3;
using Amazon.S3.Model;
using CreoHub.Application.Commands.StorageCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Queries.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("[controller]")]
public class S3Controller : ControllerBase
{
    private static readonly Dictionary<string, long> Limits = new()
    {
        { "video",       50L  * 1024 * 1024 },   // 50MB
        { "image",       5L   * 1024 * 1024 },   // 5MB
        { "application", 2L * 1024 * 1024 * 1024 },  // 2GB
    };

    public static long GetLimit(string mimeType)
    {
        var category = mimeType.Split('/')[0]; // "video/mp4" → "video"
    
        return Limits.TryGetValue(category, out var limit)
            ? limit
            : 10L * 1024 * 1024; // 10MB по умолчанию
    }
    private readonly IMediator _mediator;
    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    protected Guid ShopId => Guid.Parse(User.FindFirst("shop_id")?.Value ?? Guid.Empty.ToString());
    public S3Controller(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [Authorize]
    [HttpPost("optimize/{storageObjectId}")]
    public async Task<IActionResult> Optimize(Guid storageObjectId)
    {
        var command = new OptimizeStorageObjectCommand(storageObjectId, ShopId);
        var response = await _mediator.Send(command);
    
        return Ok(response);
    }

    [Authorize]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024)]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {   
        using var stream = file.OpenReadStream();
        var fileSize = stream.Length;
        var limit = GetLimit(file.ContentType);
        if (fileSize > limit)
        {
            var limitMb = limit / 1024 / 1024;
            return Ok(BaseResponse<bool>.Fail($"Файл слишком большоий. Максимум для {file.ContentType}: {limitMb}MB"));
        }
        var command = new UploadStorageObjectCommand(stream, file.FileName, file.ContentType, ShopId);

        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var command = new DeleteFile(id, ShopId);
        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles()
    {
        var query = new GetStorageObjectsQuery(ShopId);

        var response = await _mediator.Send(query);

        return Ok(response);
    }
    
    [Authorize]
    [HttpPost("attachMedia")]
    public async Task<IActionResult> AttachMedia([FromBody] AttachMediaDTO attachMediaDTO)
    {
        var command = new AttachMedia(ShopId, attachMediaDTO.ProductId, attachMediaDTO.StorageObjectId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("detachMedia")]
    public async Task<IActionResult> DetachMedia([FromBody] DetachMediaDTO detachMediaDTO)
    {
        var command = new DetachMedia(ShopId, detachMediaDTO.StorageObjectId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("attachContent")]
    public async Task<IActionResult> AttachContent([FromBody] AttachContentFileDTO dto)
    {
        var command = new AttachContentFileCommand(ShopId, dto);
        var response = await _mediator.Send(command);
        return Ok(response);
    }
    
    [Authorize]
    [HttpDelete("detachContent")]
    public async Task<IActionResult> DetachContent([FromBody] DetachContentFileDTO dto)
    {
        var command = new DetachContentFileCommand(ShopId, dto);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Обновить порядок отображения медиа-превью продукта (SortOrder).
    /// Body: { "productId": 5, "storageObjectId": "guid", "sortOrder": 2 }
    /// </summary>
    [Authorize]
    [HttpPatch("media/sort-order")]
    public async Task<IActionResult> UpdateMediaSortOrder([FromBody] UpdateMediaSortOrderDTO dto)
    {
        var response = await _mediator.Send(
            new UpdateMediaSortOrderCommand(ShopId, dto.ProductId, dto.StorageObjectId, dto.SortOrder));
        return Ok(response);
    }

    /// <summary>
    /// Редактировать ContentFile: имя и/или PriceWeight (1–10).
    /// Body: { "previewName": "...", "priceWeight": 7 } — null = не менять.
    /// </summary>
    [Authorize]
    [HttpPatch("content/{contentFileId}")]
    public async Task<IActionResult> UpdateContentFile(
        [FromRoute] Guid contentFileId,
        [FromBody] UpdateContentFileDTO dto)
    {
        var response = await _mediator.Send(
            new UpdateContentFileCommand(ShopId, contentFileId, dto));
        return Ok(response);
    }

    /// <summary>
    /// Одноразовый admin-endpoint: генерирует thumbnail для всех видео шопа у которых его нет.
    /// Возвращает { total, succeeded, failed, errors[] }. Блокирующий вызов.
    /// </summary>
    [Authorize]
    [HttpPost("backfill-thumbnails")]
    public async Task<IActionResult> BackfillThumbnails()
    {
        var response = await _mediator.Send(new BackfillThumbnailsCommand(ShopId));
        return Ok(response);
    }
}
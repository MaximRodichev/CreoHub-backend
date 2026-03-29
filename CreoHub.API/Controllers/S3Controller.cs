
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
        { "application", 1024L * 1024 * 1024 },  // 1GB
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
    
        if (response.Status == ResponseStatus.Error)
            return BadRequest(new { error = response.ErrorMessage });
    
        return Ok(response);
    }

    [Authorize]
    [RequestSizeLimit(1024 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1073741824)]
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
}
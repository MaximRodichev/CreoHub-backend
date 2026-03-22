
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
    
    private readonly IMediator _mediator;
    protected Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
    protected Guid ShopId => Guid.Parse(User.FindFirst("shop_id")?.Value ?? Guid.Empty.ToString());
    public S3Controller(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost("{id:int}/media")]
    public async Task<IActionResult> UploadMedia(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");

        using var stream = file.OpenReadStream();

        var command = new UploadProductMediaCommand(stream, file.FileName, id);
        
        await _mediator.Send(command);

        return Accepted(new { Message = "Загрузка началась, видео обрабатывается" });
    }


    [Authorize]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {   
        using var stream = file.OpenReadStream();
        var command = new UploadStorageObjectCommand(stream, file.FileName, file.ContentType, ShopId);

        var response = await _mediator.Send(command);

        return Ok(response);
    }

    [Authorize]
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var command = new DeleteFileCommand(id, ShopId);
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
        var command = new AttachMediaCommand(ShopId, attachMediaDTO.ProductId, attachMediaDTO.StorageObjectId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete("detachMedia")]
    public async Task<IActionResult> DetachMedia([FromBody] DetachMediaDTO detachMediaDTO)
    {
        var command = new DetachMediaCommand(ShopId, detachMediaDTO.StorageObjectId);
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}
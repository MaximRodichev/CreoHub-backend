
using Amazon.S3;
using Amazon.S3.Model;
using CreoHub.Application.Commands.StorageCommands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("[controller]")]
public class S3Controller : ControllerBase
{
    
    private readonly IMediator _mediator;
    public S3Controller(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost("{id:int}/media")]
    public async Task<IActionResult> UploadMedia(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");

        // Открываем поток файла
        using var stream = file.OpenReadStream();
        
        // Создаем команду
        var command = new UploadProductMediaCommand(stream, file.FileName, id);
        
        // Отправляем в MediatR
        await _mediator.Send(command);

        // Возвращаем 202 Accepted, так как процесс обработки идет в фоне
        return Accepted(new { Message = "Загрузка началась, видео обрабатывается" });
    }
}
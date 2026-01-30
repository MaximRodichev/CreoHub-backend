using System.Text.Json;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Tag;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")] 
public class TagController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public TagController(IMediator mediator)
    {
        _mediator=mediator;
    }

    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetTagsListCommand();
        var response = await _mediator.Send(query);
        if (response.Status == ResponseStatus.Error)
        {
            return BadRequest(response.ErrorMessage);
        }
        return Ok(response);
    }
}
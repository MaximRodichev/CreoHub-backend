using System.Security.Claims;
using AutoMapper;
using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProductRepository _productRepository;

    public ProductController(IMediator mediator, IProductRepository productRepository)
    {
        _mediator = mediator;
        _productRepository = productRepository;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProductDTO dto)
    {
        Guid id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var command = new CreateProductCommand(id, dto);
        var response = await _mediator.Send(command);
        
        if(response.Status== ResponseStatus.Error)
            return BadRequest(response.ErrorMessage);
        
        return Ok(response);
    }
}
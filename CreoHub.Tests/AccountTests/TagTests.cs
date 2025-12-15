using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Queries.Shop;
using CreoHub.Application.Queries.Tag;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CreoHub.Tests.AccountTests;

public class TagTests : IDisposable
{
    private ServiceProvider _provider;
    private readonly ITestOutputHelper? _output;
    private readonly IMediator? _mediator;

    private Guid createdUserGuid;
    
    public TagTests(ITestOutputHelper output)
    {
        var services = Initializer.InitServices();

        _provider = services.BuildServiceProvider();
        _output = output;
    }
    
    public CreateProductDTO data =  new CreateProductDTO()
    {
        Name = "InOut ChickenRoad",
        Description = "Animations + static + sound",
        Price = 25,
        Tags = new List<int>(){1,2,3}
    };

    [Theory]
    [InlineData("pragmatic play")]
    [InlineData("netent")]
    [InlineData("evolution")]
    [InlineData("microgaming")]
    [InlineData("isoftbet")]
    public async Task CreateTag(string tagName)
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var command = new CreateTagCommand(tagName);

        var response = await mediator.Send(command);
        
        _output.WriteLine(response.ToString());
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetTagsList()
    {
        using var scope = _provider.CreateScope(); 
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetTagsListCommand();

        var response = await mediator.Send(query);
        
        _output.WriteLine(response.ToString());
        _output.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
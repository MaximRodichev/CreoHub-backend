using System.Text.Json;
using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Queries.Shop;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CreoHub.Tests.AccountTests;

public class ShopTests : IDisposable
{
    private ServiceProvider _provider;
    private readonly ITestOutputHelper? _output;
    private readonly IMediator? _mediator;

    private Guid createdUserGuid;
    
    public ShopTests(ITestOutputHelper output)
    {
        var services = Initializer.InitServices();

        _provider = services.BuildServiceProvider();
        _output = output;
    }
    
    public CreateShopDTO data =  new CreateShopDTO()
    {
        Name = "CreoIshodniki",
        Description = "Delaem lybie sloty pod vash zapros"
    };

    [Fact]
    public async Task CreateShop()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        //
        var query = new GetAccountsQuery();
        var responseUsers = await mediator.Send(query);
        
        var command = new CreateShopCommand(responseUsers.Data[0].Id, data);

        var response = await mediator.Send(command);
        
        _output.WriteLine(response.ToString());
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetAllShops()
    {
        using var scope = _provider.CreateScope(); 
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetShopsShortInfoQuery();

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
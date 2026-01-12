using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Repositories;
using CreoHub.Infrastructure.Persistence;
using CreoHub.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using System.Text.Json;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Queries.Product;
using CreoHub.Domain.Entities;


namespace CreoHub.Tests.AccountTests;

public class OrderTests : IDisposable
{
    private ServiceProvider _provider;
    private readonly ITestOutputHelper? _output;
    private readonly IMediator? _mediator;
    
    public OrderTests(ITestOutputHelper output)
    {
        var services = Initializer.InitServices();

        _provider = services.BuildServiceProvider();
        _output = output;
    }
    

    private static List<Guid> Orders =  new List<Guid>();

    //TODO: нельзя купить одно и тоже дважды ( по идее на уровне БД это уник ключ {productId, customerId}
    [Fact]
    public async Task CreateOrder()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new GetAccountsQuery();
        var responseUsers = await mediator.Send(query);
        
        var query2 = new GetProductsByFilterQuery(new FiltersDto(){});
        var responseProducts = await mediator.Send(query2);
        
        var createOrderDto = new CreateOrderDTO()
        {
            Date = DateTime.Now,
            ProductId = responseProducts.Data[0].Id,
        };
        
        var command = new CreateOrderCommand(responseUsers.Data[1].Id, createOrderDto);

        var response = await mediator.Send(command);
        
        _output.WriteLine(response.ToString());
        Assert.Equal(ResponseStatus.Success, response.Status);
    }

    [Fact]
    public async Task GetAllOrders()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new GetOrdersQuery();
        
        var response = await mediator.Send(query);

        _output.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }
    
    [Theory]
    [InlineData("0b12aecf-bb25-40ef-a374-8484e3e85ea5", "a2a26a29-8fb2-41eb-8e09-061c75d071b9")]
    public async Task GetOrderFullInfo(Guid customerId, Guid orderId)
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new GetOrderFullInfoQuery(customerId, orderId);
        
        var response = await mediator.Send(query);

        _output.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));
        
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }
    
    public void Dispose()
    {
        _provider.Dispose();
    }
}
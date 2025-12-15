using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
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

public class ProductTests : IDisposable
{
    private ServiceProvider _provider;
    private readonly ITestOutputHelper? _output;
    private readonly IMediator? _mediator;

    private Guid createdUserGuid;
    
    public ProductTests(ITestOutputHelper output)
    {
        var services = Initializer.InitServices();

        _provider = services.BuildServiceProvider();
        _output = output;
    }
    
    public static IEnumerable<object[]> CreateProductData =>
        new List<object[]>
        {
            new object[] { "InOut ChickenRoad", "Animations+Static+sound", 25m, new List<int>{1,3} },
            new object[] { "InOut CricketRoad", "Animations+Static+sound", 12m, new List<int>{2} },
            new object[] { "InOut TwistXmas", "Animations+Static+sound", 10m, new List<int>{4,5} },
        };

    [Theory]
    [MemberData(nameof(CreateProductData))]
    public async Task CreateProduct(string name, string description, decimal price, List<int> tags)
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new GetAccountsQuery();
        var responseUsers = await mediator.Send(query);

        var tagsQuery = new GetTagsListCommand();
        var responseTags = await mediator.Send(tagsQuery);
        
        CreateProductDTO data =  new CreateProductDTO()
        {
            Name = name,
            Description = description,
            Price = price,
            Tags = tags,
            Date = DateTime.Now
        };
        
        var command = new CreateProductCommand(responseUsers.Data[0].Id, data);

        var response = await mediator.Send(command);
        
        _output.WriteLine(response.ToString());
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetAllProducts()
    {
        using var scope = _provider.CreateScope(); 
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetProductsByFilterQuery(new FiltersDto()
        {
            Page = 1,
            PageSize = 1
        });

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
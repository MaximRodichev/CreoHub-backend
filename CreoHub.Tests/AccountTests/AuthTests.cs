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
using CreoHub.Domain.Entities;


namespace CreoHub.Tests.AccountTests;

public class AuthTests : IDisposable
{
    private ServiceProvider _provider;
    private readonly ITestOutputHelper? _output;
    private readonly IMediator? _mediator;

    private Guid createdUserGuid;
    
    public AuthTests(ITestOutputHelper output)
    {
        var services = Initializer.InitServices();

        _provider = services.BuildServiceProvider();
        _output = output;
    }

    private AuthAccountDTO userData1 = new AuthAccountDTO()
    {
        Email = null,
        Name = "Test User Telegram",
        TelegramId = Random.Shared.NextInt64(100000000, 900000000)
    };

    private AuthAccountDTO userData2 = new AuthAccountDTO()
    {
        Email = "vymasfayaya@gmail.com",
        Name = "Vymasfayaya",
        TelegramId = null
    };


    [Fact]
    public async Task CreateAccount_ViaTelegram_Correct()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var command = new AuthAccountCommand(userData1);

        var response = await mediator.Send(command);
        
        _output.WriteLine(response.ToString());
        createdUserGuid = response.Data.Id;
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }
    
    [Fact]
    public async Task CreateAccount_ViaGoogle_Correct()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        
        var command = new AuthAccountCommand(userData2);

        var response = await mediator.Send(command);
        
        _output.WriteLine(response.ToString());
        Assert.Equal(ResponseStatus.Success, response.Status);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task GetAllAccounts()
    {
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        var query = new GetAccountsQuery();
        
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
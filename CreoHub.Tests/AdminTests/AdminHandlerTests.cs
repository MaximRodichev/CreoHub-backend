using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Queries.Admin;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.AdminTests;

public class AdminHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IAdminRepository   _adminRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IUnitOfWork        _unitOfWork;

    private static readonly Guid UserId = Guid.Parse("aaaa0000-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ShopId = Guid.Parse("bbbb0000-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public AdminHandlerTests(ITestOutputHelper output)
    {
        _output      = output;
        _adminRepo   = Substitute.For<IAdminRepository>();
        _accountRepo = Substitute.For<IAccountRepository>();
        _unitOfWork  = Substitute.For<IUnitOfWork>();
    }

    // ── GetAdminUsers ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminUsers_ReturnsAllUsers()
    {
        var users = new List<AdminUserDto>
        {
            new(UserId, "MaxG", "max@gmail.com", "Admin", 250m, 5m, 3, DateTime.UtcNow.AddDays(-100)),
            new(Guid.NewGuid(), "Bob", null, "User", 0m, 0m, 0, DateTime.UtcNow.AddDays(-10)),
        };
        _adminRepo.GetAllUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        var handler = new GetAdminUsersHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("MaxG", result.Data[0].Name);
    }

    [Fact]
    public async Task GetAdminUsers_EmptyList_ReturnsSuccess()
    {
        _adminRepo.GetAllUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<AdminUserDto>()));

        var handler = new GetAdminUsersHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAdminUsers_RepositoryThrows_ReturnsError()
    {
        _adminRepo.GetAllUsersAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("DB connection lost"));

        var handler = new GetAdminUsersHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB connection lost", result.ErrorMessage);
    }

    // ── GetAdminUserDetail ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminUserDetail_ExistingUser_ReturnsDetail()
    {
        var detail = new AdminUserDetailDto(
            UserId, "MaxG", "max@gmail.com", null, "Admin", 500m, 10m,
            DateTime.UtcNow.AddDays(-200),
            new List<AdminOrderSummaryDto>
            {
                new(Guid.NewGuid(), 25m, "Completed", DateTime.UtcNow.AddDays(-3), 2),
                new(Guid.NewGuid(), 10m, "Completed", DateTime.UtcNow.AddDays(-10), 1),
            }
        );
        _adminRepo.GetUserDetailAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AdminUserDetailDto?>(detail));

        var handler = new GetAdminUserDetailHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminUserDetailQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Orders: {result.Data?.RecentOrders.Count}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("MaxG", result.Data!.Name);
        Assert.Equal(2, result.Data.RecentOrders.Count);
        Assert.Equal(500m, result.Data.LifetimeSpent);
    }

    [Fact]
    public async Task GetAdminUserDetail_UserNotFound_ReturnsError()
    {
        _adminRepo.GetUserDetailAsync(UserId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var handler = new GetAdminUserDetailHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminUserDetailQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task GetAdminUserDetail_RepositoryThrows_ReturnsError()
    {
        _adminRepo.GetUserDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Timeout"));

        var handler = new GetAdminUserDetailHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminUserDetailQuery(UserId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetAdminShops ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminShops_ReturnsAllShops()
    {
        var shops = new List<AdminShopDto>
        {
            new(ShopId, "GamblElements", "Slot assets", "MaxG", UserId, 15, 1200m, DateTime.UtcNow.AddDays(-90)),
            new(Guid.NewGuid(), "CreoStore", "Motion assets", "Bob", Guid.NewGuid(), 5, 300m, DateTime.UtcNow.AddDays(-30)),
        };
        _adminRepo.GetAllShopsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(shops));

        var handler = new GetAdminShopsHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminShopsQuery(), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(1200m, result.Data[0].TotalRevenue);
    }

    [Fact]
    public async Task GetAdminShops_RepositoryThrows_ReturnsError()
    {
        _adminRepo.GetAllShopsAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("Schema error"));

        var handler = new GetAdminShopsHandler(_adminRepo);
        var result  = await handler.Handle(new GetAdminShopsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Schema error", result.ErrorMessage);
    }

    // ── CreateClient ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateClient_ValidName_CreatesUserAndReturnsId()
    {
        _accountRepo.AddAsync(Arg.Any<User>()).Returns(ci => Task.FromResult(ci.Arg<User>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new CreateClientHandler(_accountRepo, _unitOfWork);
        var result  = await handler.Handle(
            new CreateClientCommand("MaxG", "max@gmail.com"), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Id: {result.Data}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotEqual(Guid.Empty, result.Data);
        await _accountRepo.Received(1).AddAsync(Arg.Is<User>(u => u.Name == "MaxG"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateClient_NoEmail_CreatesUserSuccessfully()
    {
        _accountRepo.AddAsync(Arg.Any<User>()).Returns(ci => Task.FromResult(ci.Arg<User>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new CreateClientHandler(_accountRepo, _unitOfWork);
        var result  = await handler.Handle(
            new CreateClientCommand("Anonymous", null), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await _accountRepo.Received(1).AddAsync(Arg.Is<User>(u => u.EmailAddress == null));
    }

    [Fact]
    public async Task CreateClient_EmptyName_ReturnsError()
    {
        var handler = new CreateClientHandler(_accountRepo, _unitOfWork);
        var result  = await handler.Handle(
            new CreateClientCommand("   ", null), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("обязательно", result.ErrorMessage);
        await _accountRepo.DidNotReceive().AddAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task CreateClient_TrimsNameWhitespace()
    {
        User? capturedUser = null;
        _accountRepo.AddAsync(Arg.Do<User>(u => capturedUser = u))
            .Returns(ci => Task.FromResult(ci.Arg<User>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new CreateClientHandler(_accountRepo, _unitOfWork);
        await handler.Handle(new CreateClientCommand("  Alice  ", null), CancellationToken.None);

        Assert.NotNull(capturedUser);
        Assert.Equal("Alice", capturedUser!.Name);
    }

    [Fact]
    public async Task CreateClient_RepositoryThrows_ReturnsError()
    {
        _accountRepo.AddAsync(Arg.Any<User>()).Throws(new Exception("Duplicate email"));

        var handler = new CreateClientHandler(_accountRepo, _unitOfWork);
        var result  = await handler.Handle(
            new CreateClientCommand("Bob", "bob@example.com"), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Duplicate email", result.ErrorMessage);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

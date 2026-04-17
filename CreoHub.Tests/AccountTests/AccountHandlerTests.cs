using AutoMapper;
using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.AccountTests;

public class AccountHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly IAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public AccountHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _accountRepo = Substitute.For<IAccountRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _mapper = Substitute.For<IMapper>();
    }

    // ── AuthAccount ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthAccount_ExistingUser_ReturnsIdentityWithoutSave()
    {
        var existingUser = User.Create("MaxG", "max@gmail.com");
        var expectedDto = new IdentityDTO { Id = existingUser.Id, Name = existingUser.Name };

        _accountRepo.FindUserByCredentials("max@gmail.com", null).Returns(Task.FromResult<User?>(existingUser));
        _mapper.Map<IdentityDTO>(existingUser).Returns(expectedDto);

        var handler = new AuthAccountHandler(_accountRepo, _unitOfWork, _mapper);
        var result = await handler.Handle(
            new AuthAccountCommand(new AuthAccountDTO { Email = "max@gmail.com", Name = "MaxG" }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Id: {result.Data?.Id}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(existingUser.Id, result.Data.Id);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthAccount_NewUser_CreatesUserAndSaves()
    {
        _accountRepo.FindUserByCredentials(null, 123456789L).ReturnsNull();
        _accountRepo.AddAsync(Arg.Any<User>()).Returns(ci => Task.FromResult(ci.Arg<User>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _mapper.Map<IdentityDTO>(Arg.Any<User>()).Returns(ci =>
            new IdentityDTO { Id = Guid.NewGuid(), Name = ci.Arg<User>().Name });

        var handler = new AuthAccountHandler(_accountRepo, _unitOfWork, _mapper);
        var result = await handler.Handle(
            new AuthAccountCommand(new AuthAccountDTO { Name = "NewUser", TelegramId = 123456789L }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Name: {result.Data?.Name}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotNull(result.Data);
        await _accountRepo.Received(1).AddAsync(Arg.Any<User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthAccount_RepositoryThrows_ReturnsError()
    {
        _accountRepo.FindUserByCredentials(Arg.Any<string?>(), Arg.Any<long?>())
            .Throws(new Exception("DB timeout"));

        var handler = new AuthAccountHandler(_accountRepo, _unitOfWork, _mapper);
        var result = await handler.Handle(
            new AuthAccountCommand(new AuthAccountDTO { Email = "x@x.com", Name = "X" }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB timeout", result.ErrorMessage);
    }

    // ── GetAccounts ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccounts_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            User.Create("Alice", "alice@gmail.com"),
            User.Create("Bob", null, 987654321L),
        };
        _accountRepo.GetAllAsync().Returns(Task.FromResult(users));

        var handler = new GetAccountsQueryHandler(_accountRepo);
        var result = await handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task GetAccounts_RepositoryThrows_ReturnsError()
    {
        _accountRepo.GetAllAsync().Throws(new Exception("Connection lost"));

        var handler = new GetAccountsQueryHandler(_accountRepo);
        var result = await handler.Handle(new GetAccountsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Connection lost", result.ErrorMessage);
    }

    // ── GetProfile ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_ReturnsUserProfile()
    {
        var profile = new UserProfileDTO
        {
            Id = UserId,
            Name = "MaxG",
            Email = "max@gmail.com",
            Role = "User"
        };
        _accountRepo.GetUserProfileByUserId(UserId).Returns(Task.FromResult(profile));

        var handler = new GetProfileQueryHandler(_accountRepo);
        var result = await handler.Handle(new GetProfileQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Name: {result.Data?.Name}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("MaxG", result.Data.Name);
        Assert.Equal("User", result.Data.Role);
    }

    [Fact]
    public async Task GetProfile_RepositoryThrows_ReturnsError()
    {
        _accountRepo.GetUserProfileByUserId(UserId).Throws(new Exception("Not found"));

        var handler = new GetProfileQueryHandler(_accountRepo);
        var result = await handler.Handle(new GetProfileQuery(UserId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }
}

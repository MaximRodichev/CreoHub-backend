using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace CreoHub.Tests.RetentionTests;

/// <summary>Менеджерское создание магазина: гарды (юзер есть / ещё нет шопа / валидация имени).</summary>
public class AdminCreateShopTests
{
    private readonly IShopRepository    _shops    = Substitute.For<IShopRepository>();
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork        _uow      = Substitute.For<IUnitOfWork>();

    private AdminCreateShopHandler Handler() => new(_shops, _accounts, _uow);

    private static readonly Guid Owner = Guid.NewGuid();

    [Fact]
    public async Task Success_CreatesShop_AssignsToUser()
    {
        _accounts.GetByIdAsync(Owner).Returns(User.Create("Buyer", "b@b.com"));
        _shops.GetShopIdByOwnerIdAsync(Owner).Returns((Guid?)null);

        var res = await Handler().Handle(
            new AdminCreateShopCommand(Owner, "My Shop", "desc", Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, res.Status);
        await _shops.Received(1).AddAsync(Arg.Any<Shop>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserNotFound_Fails()
    {
        _accounts.GetByIdAsync(Owner).ReturnsNull();
        var res = await Handler().Handle(
            new AdminCreateShopCommand(Owner, "My Shop", "desc", Guid.NewGuid()), CancellationToken.None);
        Assert.Equal(ResponseStatus.Error, res.Status);
    }

    [Fact]
    public async Task AlreadyHasShop_Fails()
    {
        _accounts.GetByIdAsync(Owner).Returns(User.Create("B", "b@b.com"));
        _shops.GetShopIdByOwnerIdAsync(Owner).Returns(Guid.NewGuid());
        var res = await Handler().Handle(
            new AdminCreateShopCommand(Owner, "My Shop", "desc", Guid.NewGuid()), CancellationToken.None);
        Assert.Equal(ResponseStatus.Error, res.Status);
        Assert.Contains("уже есть магазин", res.ErrorMessage ?? "");
    }

    [Fact]
    public async Task ShortName_Fails_NoUserLoad()
    {
        var res = await Handler().Handle(
            new AdminCreateShopCommand(Owner, "ab", "desc", Guid.NewGuid()), CancellationToken.None);
        Assert.Equal(ResponseStatus.Error, res.Status);
        await _shops.DidNotReceive().AddAsync(Arg.Any<Shop>());
    }
}

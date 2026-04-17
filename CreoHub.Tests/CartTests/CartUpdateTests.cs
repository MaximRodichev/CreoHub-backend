using CreoHub.Application.Commands.CartCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.CartTests;

public class CartUpdateTests
{
    private readonly ITestOutputHelper _output;
    private readonly ICartRepository _cartRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid UserId     = Guid.Parse("dddd0001-0000-0000-0000-000000000000");
    private static readonly Guid CartId     = Guid.Parse("dddd0002-0000-0000-0000-000000000000");
    private static readonly Guid CartItemId = Guid.Parse("dddd0003-0000-0000-0000-000000000000");

    public CartUpdateTests(ITestOutputHelper output)
    {
        _output = output;
        _cartRepo  = Substitute.For<ICartRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    // ── Domain: CartItem.UpdateFiles ──────────────────────────────────────────

    [Fact]
    public void UpdateFiles_ReplacesAllSelectedFiles()
    {
        var item = CartItem.Create(CartId, 42,
            [Guid.NewGuid(), Guid.NewGuid()]); // 2 файла изначально

        var newFiles = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        item.UpdateFiles(newFiles);

        Assert.Equal(3, item.SelectedFiles.Count);
        foreach (var id in newFiles)
            Assert.Contains(item.SelectedFiles, f => f.ContentFileId == id);
    }

    [Fact]
    public void UpdateFiles_WithEmptyList_ClearsAllFiles()
    {
        var item = CartItem.Create(CartId, 42, [Guid.NewGuid(), Guid.NewGuid()]);
        item.UpdateFiles(new List<Guid>());
        Assert.Empty(item.SelectedFiles);
    }

    [Fact]
    public void UpdateFiles_CalledTwice_OnlySecondCallPersists()
    {
        var item = CartItem.Create(CartId, 42, [Guid.NewGuid()]);

        var first  = new List<Guid> { Guid.NewGuid() };
        var second = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        item.UpdateFiles(first);
        item.UpdateFiles(second);

        Assert.Equal(2, item.SelectedFiles.Count);
        foreach (var id in second)
            Assert.Contains(item.SelectedFiles, f => f.ContentFileId == id);
    }

    // ── Handler: UpdateCartItemFilesHandler ───────────────────────────────────

    private static CartItem MakeCartItem(Guid cartItemId, Guid cartId)
    {
        var item = CartItem.Create(cartId, 10, [Guid.NewGuid()]);
        typeof(CartItem).GetProperty("Id")!.SetValue(item, cartItemId);
        return item;
    }

    [Fact]
    public async Task UpdateCartItemFiles_OwnerAndValidItem_Succeeds()
    {
        var cart     = Cart.Create(UserId);
        typeof(Cart).GetProperty("Id")!.SetValue(cart, CartId);
        var cartItem = MakeCartItem(CartItemId, CartId);
        var newFiles = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _cartRepo.GetCartItemWithFilesAsync(CartItemId).Returns(cartItem);
        _cartRepo.GetByUserIdAsync(UserId).Returns(cart);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateCartItemFilesHandler(_cartRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateCartItemFilesCommand(UserId, CartItemId, newFiles), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCartItemFiles_CartItemNotFound_ReturnsError()
    {
        _cartRepo.GetCartItemWithFilesAsync(CartItemId).ReturnsNull();

        var handler = new UpdateCartItemFilesHandler(_cartRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateCartItemFilesCommand(UserId, CartItemId, [Guid.NewGuid()]),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCartItemFiles_CartItemBelongsToAnotherUser_ReturnsAccessDenied()
    {
        var otherCartId = Guid.NewGuid();
        var cartItem    = MakeCartItem(CartItemId, otherCartId); // чужой cart

        var myCart = Cart.Create(UserId);
        typeof(Cart).GetProperty("Id")!.SetValue(myCart, CartId); // CartId ≠ otherCartId

        _cartRepo.GetCartItemWithFilesAsync(CartItemId).Returns(cartItem);
        _cartRepo.GetByUserIdAsync(UserId).Returns(myCart);

        var handler = new UpdateCartItemFilesHandler(_cartRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateCartItemFilesCommand(UserId, CartItemId, [Guid.NewGuid()]),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCartItemFiles_WithEmptyList_SetsFullPurchase()
    {
        var cart     = Cart.Create(UserId);
        typeof(Cart).GetProperty("Id")!.SetValue(cart, CartId);
        var cartItem = MakeCartItem(CartItemId, CartId);

        _cartRepo.GetCartItemWithFilesAsync(CartItemId).Returns(cartItem);
        _cartRepo.GetByUserIdAsync(UserId).Returns(cart);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateCartItemFilesHandler(_cartRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateCartItemFilesCommand(UserId, CartItemId, new List<Guid>()),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(cartItem.SelectedFiles); // пустой = полная покупка
    }

    // ── Handler: ClearCartHandler ─────────────────────────────────────────────

    [Fact]
    public async Task ClearCart_Always_CallsClearAndSave()
    {
        _cartRepo.ClearCartAsync(UserId).Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ClearCartHandler(_cartRepo, _unitOfWork);
        var result  = await handler.Handle(new ClearCartCommand(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _cartRepo.Received(1).ClearCartAsync(UserId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearCart_WhenRepositoryThrows_ReturnsError()
    {
        _cartRepo.ClearCartAsync(UserId).Throws(new Exception("DB error"));

        var handler = new ClearCartHandler(_cartRepo, _unitOfWork);
        var result  = await handler.Handle(new ClearCartCommand(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB error", result.ErrorMessage);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

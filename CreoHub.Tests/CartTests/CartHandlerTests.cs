using CreoHub.Application.Commands.CartCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Cart;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.CartTests;

public class CartHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly ICartRepository _cartRepo;
    private readonly IProductRepository _productRepo;
    private readonly IMediaProductRepository _mediaProductRepo;
    private readonly IContentFileRepository _contentFileRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const int ProductId = 42;

    public CartHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _cartRepo = Substitute.For<ICartRepository>();
        _productRepo = Substitute.For<IProductRepository>();
        _mediaProductRepo = Substitute.For<IMediaProductRepository>();
        _contentFileRepo = Substitute.For<IContentFileRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    // ── ToggleCartItem ────────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleCartItem_WhenItemNotInCart_AddsItem()
    {
        var cart = Cart.Create(UserId);
        var contentFiles = new List<ContentFile>
        {
            new ContentFile(1, "file1.zip", Guid.NewGuid(), ProductId),
            new ContentFile(3, "file2.zip", Guid.NewGuid(), ProductId),
        };

        _cartRepo.GetCartItemByUserAndProduct(UserId, ProductId).ReturnsNull();
        _cartRepo.GetByUserIdAsync(UserId).Returns(cart);
        _productRepo.GetContentFilesOfProduct(ProductId).Returns(contentFiles);
        _cartRepo.AddCartItem(Arg.Any<CartItem>()).Returns(true);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new ToggleCartItemHandler(_cartRepo, _unitOfWork, _productRepo);
        var result = await handler.Handle(new ToggleCartItemQuery(UserId, ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _cartRepo.Received(1).AddCartItem(Arg.Is<CartItem>(ci => ci.ProductId == ProductId));
        await _cartRepo.DidNotReceive().RemoveCartItem(Arg.Any<CartItem>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleCartItem_WhenItemAlreadyInCart_RemovesItem()
    {
        var cartId = Guid.NewGuid();
        var existingItem = CartItem.Create(cartId, ProductId, [Guid.NewGuid()]);

        _cartRepo.GetCartItemByUserAndProduct(UserId, ProductId).Returns(Task.FromResult<CartItem?>(existingItem));
        _cartRepo.RemoveCartItem(Arg.Any<CartItem>()).Returns(Task.FromResult(true));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new ToggleCartItemHandler(_cartRepo, _unitOfWork, _productRepo);
        var result = await handler.Handle(new ToggleCartItemQuery(UserId, ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _cartRepo.Received(1).RemoveCartItem(existingItem);
        await _cartRepo.DidNotReceive().AddCartItem(Arg.Any<CartItem>());
    }

    [Fact]
    public async Task ToggleCartItem_WhenRepositoryThrows_ReturnsError()
    {
        _cartRepo.GetCartItemByUserAndProduct(UserId, ProductId)
            .Throws(new Exception("DB connection failed"));

        var handler = new ToggleCartItemHandler(_cartRepo, _unitOfWork, _productRepo);
        var result = await handler.Handle(new ToggleCartItemQuery(UserId, ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB connection failed", result.ErrorMessage);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── GetCart ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCart_WithItems_ReturnsMappedDTOs()
    {
        var cart = Cart.Create(UserId);
        var cartItem = CartItem.Create(cart.Id, ProductId, [Guid.NewGuid(), Guid.NewGuid()]);
        cart.Items.Add(cartItem); // ICollection with private set — reference is writable

        var contentFiles = new List<ContentFile>
        {
            new ContentFile(1, "preview1.png", Guid.NewGuid(), ProductId),
            new ContentFile(5, "preview2.png", Guid.NewGuid(), ProductId),
        };
        var previewKeys = new Dictionary<int, string?> { { ProductId, "products/42/thumb.webp" } };

        _cartRepo.GetFullCartAsync(UserId).Returns(cart);
        _contentFileRepo.GetByProductIdsAsync(Arg.Any<IEnumerable<int>>()).Returns(contentFiles);
        _mediaProductRepo.GetPreviewKeysByProductIds(Arg.Any<IEnumerable<int>>()).Returns(previewKeys);

        var handler = new GetCartHandler(_cartRepo, _mediaProductRepo, _contentFileRepo);
        var result = await handler.Handle(new GetCartQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Items count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Single(result.Data);

        var dto = result.Data[0];
        Assert.Equal(ProductId, dto.ProductId);
        Assert.Equal("products/42/thumb.webp", dto.PreviewKey);
        Assert.Equal(2, dto.ContentFileInfos.Count);
        Assert.Equal(cartItem.Id, dto.CartItemId);
    }

    [Fact]
    public async Task GetCart_EmptyCart_ReturnsEmptyList()
    {
        var cart = Cart.Create(UserId);

        _cartRepo.GetFullCartAsync(UserId).Returns(cart);
        _contentFileRepo.GetByProductIdsAsync(Arg.Any<IEnumerable<int>>()).Returns(new List<ContentFile>());
        _mediaProductRepo.GetPreviewKeysByProductIds(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, string?>());

        var handler = new GetCartHandler(_cartRepo, _mediaProductRepo, _contentFileRepo);
        var result = await handler.Handle(new GetCartQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Items count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetCart_WhenRepositoryThrows_ReturnsError()
    {
        _cartRepo.GetFullCartAsync(UserId).Throws(new Exception("Cart not found"));

        var handler = new GetCartHandler(_cartRepo, _mediaProductRepo, _contentFileRepo);
        var result = await handler.Handle(new GetCartQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Cart not found", result.ErrorMessage);
    }
}

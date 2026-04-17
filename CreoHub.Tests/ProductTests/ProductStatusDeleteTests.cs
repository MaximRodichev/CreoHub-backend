using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ProductTests;

public class ProductStatusDeleteTests
{
    private readonly ITestOutputHelper _output;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid ShopId      = Guid.Parse("eeee0001-0000-0000-0000-000000000000");
    private static readonly Guid OtherShopId = Guid.Parse("eeee0002-0000-0000-0000-000000000000");
    private static readonly Guid OwnerId     = Guid.Parse("eeee0003-0000-0000-0000-000000000000");
    private const int ProductId = 99;

    public ProductStatusDeleteTests(ITestOutputHelper output)
    {
        _output     = output;
        _productRepo = Substitute.For<IProductRepository>();
        _unitOfWork  = Substitute.For<IUnitOfWork>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Product MakeProduct(Guid shopId, ProductStatus status = ProductStatus.Active)
    {
        var product = new Product("Test Product", "Description", shopId);
        // Если нужен Hidden — один раз Hide() (через рефлексию т.к. приватное состояние)
        if (status == ProductStatus.Hidden)
            typeof(Product)
                .GetProperty("ProductStatus")!
                .SetValue(product, ProductStatus.Hidden);
        return product;
    }

    // ── Domain: Product.Activate / Product.Hide ───────────────────────────────

    [Fact]
    public void Product_Hide_ActiveProduct_ChangesStatusToHidden()
    {
        var product = new Product("Test", "Desc", ShopId);
        Assert.Equal(ProductStatus.Active, product.ProductStatus);

        product.Hide();

        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
    }

    [Fact]
    public void Product_Activate_HiddenProduct_ChangesStatusToActive()
    {
        var product = MakeProduct(ShopId, ProductStatus.Hidden);
        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);

        product.Activate();

        Assert.Equal(ProductStatus.Active, product.ProductStatus);
    }

    [Fact]
    public void Product_Hide_AlreadyHidden_Throws()
    {
        var product = MakeProduct(ShopId, ProductStatus.Hidden);
        Assert.Throws<InvalidOperationException>(() => product.Hide());
    }

    [Fact]
    public void Product_Activate_AlreadyActive_Throws()
    {
        var product = new Product("Test", "Desc", ShopId);
        Assert.Throws<InvalidOperationException>(() => product.Activate());
    }

    // ── Handler: ChangeProductStatusHandler ───────────────────────────────────

    [Fact]
    public async Task ChangeStatus_ToHidden_ActiveProduct_Succeeds()
    {
        var product = MakeProduct(ShopId, ProductStatus.Active);
        _productRepo.GetProductById(ProductId).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ChangeProductStatusHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new ChangeProductStatusCommand(ShopId, ProductId, "Hidden"),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_ToActive_HiddenProduct_Succeeds()
    {
        var product = MakeProduct(ShopId, ProductStatus.Hidden);
        _productRepo.GetProductById(ProductId).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ChangeProductStatusHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new ChangeProductStatusCommand(ShopId, ProductId, "Active"),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Active, product.ProductStatus);
    }

    [Fact]
    public async Task ChangeStatus_ProductNotFound_ReturnsError()
    {
        _productRepo.GetProductById(ProductId).ReturnsNull();

        var handler = new ChangeProductStatusHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new ChangeProductStatusCommand(ShopId, ProductId, "Hidden"),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_WrongShopId_ReturnsAccessDenied()
    {
        var product = MakeProduct(OtherShopId); // принадлежит другому шопу
        _productRepo.GetProductById(ProductId).Returns(product);

        var handler = new ChangeProductStatusHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new ChangeProductStatusCommand(ShopId, ProductId, "Hidden"),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_InvalidTargetStatus_ReturnsError()
    {
        var product = MakeProduct(ShopId);
        _productRepo.GetProductById(ProductId).Returns(product);

        var handler = new ChangeProductStatusHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new ChangeProductStatusCommand(ShopId, ProductId, "Deleted"), // неверный статус
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Invalid status", result.ErrorMessage);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_CaseInsensitive_Succeeds()
    {
        var product = MakeProduct(ShopId, ProductStatus.Active);
        _productRepo.GetProductById(ProductId).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ChangeProductStatusHandler(_productRepo, _unitOfWork);

        // "HIDDEN", "hidden", "Hidden" — все должны работать
        var result = await handler.Handle(
            new ChangeProductStatusCommand(ShopId, ProductId, "HIDDEN"),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
    }

    // ── Handler: DeleteProductHandler ─────────────────────────────────────────

    [Fact]
    public async Task DeleteProduct_ActiveProduct_HidesIt()
    {
        var product = MakeProduct(ShopId, ProductStatus.Active);
        _productRepo.GetProductById(ProductId).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new DeleteProductHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new DeleteProductCommand(ShopId, ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProduct_AlreadyHiddenProduct_DoesNotThrow()
    {
        // Soft delete на уже скрытый продукт — идемпотентная операция
        var product = MakeProduct(ShopId, ProductStatus.Hidden);
        _productRepo.GetProductById(ProductId).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new DeleteProductHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new DeleteProductCommand(ShopId, ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        // Продукт уже Hidden — ничего не сломалось
        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
    }

    [Fact]
    public async Task DeleteProduct_ProductNotFound_ReturnsError()
    {
        _productRepo.GetProductById(ProductId).ReturnsNull();

        var handler = new DeleteProductHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new DeleteProductCommand(ShopId, ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteProduct_WrongShopId_ReturnsAccessDenied()
    {
        var product = MakeProduct(OtherShopId);
        _productRepo.GetProductById(ProductId).Returns(product);

        var handler = new DeleteProductHandler(_productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new DeleteProductCommand(ShopId, ProductId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}

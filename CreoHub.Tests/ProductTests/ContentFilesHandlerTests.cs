using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ProductTests;

public class ContentFilesHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly IProductRepository _productRepo;

    private static readonly Guid ShopId = Guid.Parse("bbbb0002-0000-0000-0000-000000000000");
    private const int ProductId = 55;

    public ContentFilesHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _productRepo = Substitute.For<IProductRepository>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Product MakeProductWithFiles(decimal price, params int[] weights)
    {
        var product = new Product("Test", "Desc", ShopId);
        product.AddPrice(price);
        foreach (var w in weights)
        {
            var storage = new StorageObject($"key/{w}", $"file{w}.zip", 1024, "application/zip", ShopId);
            var file = new ContentFile(w, $"file_{w}.zip", storage.Id, 0);
            product.AddContentFile(file);
        }
        return product;
    }

    // ── Tests: GetProductContentFilesHandler ──────────────────────────────────

    [Fact]
    public async Task GetContentFiles_SingleFile_ReturnedInList()
    {
        var product = MakeProductWithFiles(100m, 5);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetContentFiles_MultipleFiles_AllReturned()
    {
        var product = MakeProductWithFiles(100m, 3, 5, 7);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(3, result.Data!.Count);
    }

    [Fact]
    public async Task GetContentFiles_DTOFieldsAreMappedCorrectly()
    {
        var product = MakeProductWithFiles(80m, 4);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        var dto = result.Data![0];
        _output.WriteLine($"FileId: {dto.FileId}, FileName: {dto.FileName}, Weight: {dto.PriceWeight}");

        Assert.NotEqual(Guid.Empty, dto.FileId);
        Assert.Equal("file_4.zip", dto.FileName);
        Assert.Equal(4, dto.PriceWeight);
    }

    [Fact]
    public async Task GetContentFiles_WeightsPreservedForAllFiles()
    {
        // Фронтенд использует веса для пропорционального отображения цены
        var product = MakeProductWithFiles(100m, 2, 6, 9);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        var weights = result.Data!.Select(f => f.PriceWeight).OrderBy(w => w).ToList();
        _output.WriteLine($"Weights: {string.Join(", ", weights)}");
        Assert.Equal(new[] { 2, 6, 9 }, weights);
    }

    [Fact]
    public async Task GetContentFiles_ProductWithNoFiles_ReturnsEmptyList()
    {
        var product = new Product("Empty", "No files", ShopId);
        product.AddPrice(50m);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetContentFiles_ProductNotFound_ReturnsError()
    {
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product>());

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetContentFiles_WhenRepositoryThrows_ReturnsError()
    {
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Throws(new Exception("DB error"));

        var handler = new GetProductContentFilesHandler(_productRepo);
        var result  = await handler.Handle(
            new GetProductContentFilesQuery(ProductId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB error", result.ErrorMessage);
    }
}

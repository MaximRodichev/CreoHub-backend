using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ProductTests;

/// <summary>
/// Тесты GetBestProductsHandler — два списка продуктов для главной страницы.
/// </summary>
public class BestProductsHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly IProductRepository _productRepo;

    public BestProductsHandlerTests(ITestOutputHelper output)
    {
        _output      = output;
        _productRepo = Substitute.For<IProductRepository>();
    }

    private static List<ProductViewDTO> MakeDtoList(int count, string prefix = "Product") =>
        Enumerable.Range(1, count)
            .Select(i => new ProductViewDTO
            {
                Id    = i,
                Name  = $"{prefix} #{i}",
                Price = i * 10m,
                Date  = DateTime.UtcNow.AddDays(-i),
                Tags  = new List<string>()
            })
            .ToList();

    // ── Успешные сценарии ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetBestProducts_ReturnsBothLists()
    {
        var newest  = MakeDtoList(6, "New");
        var popular = MakeDtoList(6, "Pop");

        _productRepo
            .GetProductsByFilters(Arg.Is<FiltersDto>(f => f.SortOrder == SortOrder.Latests))
            .Returns((newest, newest.Count));
        _productRepo
            .GetProductsByFilters(Arg.Is<FiltersDto>(f => f.SortOrder == SortOrder.Popularity))
            .Returns((popular, popular.Count));

        var handler = new GetBestProductsHandler(_productRepo);
        var result  = await handler.Handle(new GetBestProductsQuery(), CancellationToken.None);

        _output.WriteLine($"Newest: {result.Data?.Newest.Count}, Popular: {result.Data?.Popular.Count}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(6, result.Data!.Newest.Count);
        Assert.Equal(6, result.Data.Popular.Count);
    }

    [Fact]
    public async Task GetBestProducts_NewestSortedByLatests()
    {
        var newest = MakeDtoList(6, "New");
        _productRepo
            .GetProductsByFilters(Arg.Is<FiltersDto>(f => f.SortOrder == SortOrder.Latests))
            .Returns((newest, 6));
        _productRepo
            .GetProductsByFilters(Arg.Is<FiltersDto>(f => f.SortOrder == SortOrder.Popularity))
            .Returns((new List<ProductViewDTO>(), 0));

        var handler = new GetBestProductsHandler(_productRepo);
        var result  = await handler.Handle(new GetBestProductsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("New #1", result.Data!.Newest[0].Name);
    }

    [Fact]
    public async Task GetBestProducts_PageSizeIs6()
    {
        _productRepo
            .GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns((new List<ProductViewDTO>(), 0));

        var handler = new GetBestProductsHandler(_productRepo);
        await handler.Handle(new GetBestProductsQuery(), CancellationToken.None);

        await _productRepo.Received(2).GetProductsByFilters(
            Arg.Is<FiltersDto>(f => f.PageSize == 6));
    }

    [Fact]
    public async Task GetBestProducts_EmptyRepository_ReturnsBothEmpty()
    {
        _productRepo
            .GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns((new List<ProductViewDTO>(), 0));

        var handler = new GetBestProductsHandler(_productRepo);
        var result  = await handler.Handle(new GetBestProductsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data!.Newest);
        Assert.Empty(result.Data.Popular);
    }

    // ── Обработка исключений ──────────────────────────────────────────────────

    [Fact]
    public async Task GetBestProducts_RepositoryThrows_ReturnsError()
    {
        _productRepo
            .GetProductsByFilters(Arg.Any<FiltersDto>())
            .ThrowsAsync(new Exception("DB connection lost"));

        var handler = new GetBestProductsHandler(_productRepo);
        var result  = await handler.Handle(new GetBestProductsQuery(), CancellationToken.None);

        _output.WriteLine($"Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB connection lost", result.ErrorMessage);
    }
}

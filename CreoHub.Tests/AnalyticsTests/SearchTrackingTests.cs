using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.AnalyticsTests;

/// <summary>
/// Tests for Block 27 search improvements:
///   - Search event tracking (search / search_no_results)
///   - Handler passes filter DTO correctly to the repository
///   - Short queries (≤2 chars) and longer queries are handled
/// </summary>
public class SearchTrackingTests
{
    private readonly ITestOutputHelper _output;

    private readonly IProductRepository _productRepo;
    private readonly IEventTracker      _events;

    private static readonly Guid UserId    = Guid.Parse("cccc0010-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid SessionId = Guid.Parse("eeee0001-eeee-eeee-eeee-eeeeeeeeeeee");
    private const string SessId = "sess-unit-test-001";

    public SearchTrackingTests(ITestOutputHelper output)
    {
        _output      = output;
        _productRepo = Substitute.For<IProductRepository>();
        _events      = Substitute.For<IEventTracker>();
    }

    // ── Search event tracking ─────────────────────────────────────────────────

    [Fact]
    public async Task Search_WithResults_TracksSearchEvent()
    {
        var products = new List<ProductViewDTO>
        {
            new() { Id = 1, Name = "ChickenRoad" }
        };
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns(Task.FromResult<(List<ProductViewDTO>, int)>((products, 1)));

        var handler = new GetProductsByFilterHandler(_productRepo, null!, _events);
        var result  = await handler.Handle(
            new GetProductsByFilterQuery(
                new FiltersDto { Search = "chicken", Page = 0, PageSize = 10 },
                UserId: UserId, SessionId: SessId),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);

        _events.Received(1).Track(
            EventTypes.Search,
            productId:  null,
            userId:     UserId,
            sessionId:  SessId,
            payload:    Arg.Any<object>());
        _events.DidNotReceive().Track(
            EventTypes.SearchNoResults,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
    }

    [Fact]
    public async Task Search_NoResults_TracksSearchNoResultsEvent()
    {
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns(Task.FromResult<(List<ProductViewDTO>, int)>((new List<ProductViewDTO>(), 0)));

        var handler = new GetProductsByFilterHandler(_productRepo, null!, _events);
        var result  = await handler.Handle(
            new GetProductsByFilterQuery(
                new FiltersDto { Search = "xyzabcdef", Page = 0, PageSize = 10 },
                UserId: UserId, SessionId: SessId),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);

        _events.Received(1).Track(
            EventTypes.SearchNoResults,
            productId:  null,
            userId:     UserId,
            sessionId:  SessId,
            payload:    Arg.Any<object>());
        _events.DidNotReceive().Track(
            EventTypes.Search,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
    }

    [Fact]
    public async Task Search_EmptySearchTerm_DoesNotTrackSearchEvent()
    {
        var products = new List<ProductViewDTO>
        {
            new() { Id = 1, Name = "SomeProduct" }
        };
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns(Task.FromResult<(List<ProductViewDTO>, int)>((products, 1)));

        var handler = new GetProductsByFilterHandler(_productRepo, null!, _events);
        await handler.Handle(
            new GetProductsByFilterQuery(new FiltersDto { Search = null, Page = 0, PageSize = 10 }),
            CancellationToken.None);

        _events.DidNotReceive().Track(
            EventTypes.Search,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
        _events.DidNotReceive().Track(
            EventTypes.SearchNoResults,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
    }

    [Fact]
    public async Task Search_WhitespaceOnlyTerm_DoesNotTrackSearchEvent()
    {
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns(Task.FromResult<(List<ProductViewDTO>, int)>((new List<ProductViewDTO>(), 0)));

        var handler = new GetProductsByFilterHandler(_productRepo, null!, _events);
        await handler.Handle(
            new GetProductsByFilterQuery(new FiltersDto { Search = "   ", Page = 0, PageSize = 10 }),
            CancellationToken.None);

        _events.DidNotReceive().Track(
            EventTypes.Search,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
        _events.DidNotReceive().Track(
            EventTypes.SearchNoResults,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
    }

    [Fact]
    public async Task Search_PassesFiltersToRepository()
    {
        // Verifies that the handler does not transform the filter dto before passing it to the repository
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns(Task.FromResult<(List<ProductViewDTO>, int)>((new List<ProductViewDTO>(), 0)));

        var filters = new FiltersDto { Search = "design", Page = 2, PageSize = 20 };
        var handler = new GetProductsByFilterHandler(_productRepo, null!, _events);
        await handler.Handle(new GetProductsByFilterQuery(filters), CancellationToken.None);

        await _productRepo.Received(1)
            .GetProductsByFilters(Arg.Is<FiltersDto>(f =>
                f.Search == "design" && f.Page == 2 && f.PageSize == 20));
    }

    [Fact]
    public async Task Search_RepositoryThrows_ReturnsError_WithoutTrackingEvent()
    {
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Throws(new Exception("DB query failed"));

        var handler = new GetProductsByFilterHandler(_productRepo, null!, _events);
        var result  = await handler.Handle(
            new GetProductsByFilterQuery(new FiltersDto { Search = "crash" }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);

        // No search tracking when repository throws
        _events.DidNotReceive().Track(
            EventTypes.Search,
            Arg.Any<int?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<object?>());
    }
}

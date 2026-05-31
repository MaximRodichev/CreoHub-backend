using System.Threading.Channels;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.AnalyticsQueries;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Infrastructure.Persistence.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.AnalyticsTests;

/// <summary>
/// Tests for Block 26 event tracking:
///   - EventTrackerService (fire-and-forget, never throws, channel write)
///   - GetShopAnalyticsHandler
///   - GetProductFunnelHandler
///   - GetAdminAnalyticsHandler
/// </summary>
public class EventTrackerTests
{
    private readonly ITestOutputHelper _output;

    private static readonly Guid ShopId    = Guid.Parse("aaaa0001-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId = Guid.Parse("bbbb0001-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public EventTrackerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── EventTrackerService ─────────────────────────────────────────────────────

    [Fact]
    public void Track_WritesToChannel()
    {
        var channel = Channel.CreateUnbounded<UserEvent>();
        var tracker = new EventTrackerService(channel);

        tracker.Track(EventTypes.ProductView, productId: 5);

        Assert.Equal(1, channel.Reader.Count);
    }

    [Fact]
    public void Track_MultipleCalls_AllWrittenToChannel()
    {
        var channel = Channel.CreateUnbounded<UserEvent>();
        var tracker = new EventTrackerService(channel);

        tracker.Track(EventTypes.ProductView,  productId: 1);
        tracker.Track(EventTypes.CartAdd,       productId: 1);
        tracker.Track(EventTypes.CheckoutStarted, productId: 2);

        Assert.Equal(3, channel.Reader.Count);
    }

    [Fact]
    public void Track_NeverThrows_EvenOnException()
    {
        // Use a completed (closed) channel so TryWrite returns false — should not throw
        var channel = Channel.CreateBounded<UserEvent>(new BoundedChannelOptions(1));
        var tracker = new EventTrackerService(channel);

        // Fill the channel to capacity
        tracker.Track(EventTypes.ProductView, productId: 1);

        // Second write into a full bounded channel — TryWrite returns false, no throw
        var exception = Record.Exception(() =>
            tracker.Track(EventTypes.CartAdd, productId: 2));

        Assert.Null(exception);
    }

    [Fact]
    public void Track_WithComplexPayload_WritesSerializedJson()
    {
        var channel = Channel.CreateUnbounded<UserEvent>();
        var tracker = new EventTrackerService(channel);

        tracker.Track(EventTypes.Search, payload: new { query = "chicken road", resultCount = 5 });

        channel.Reader.TryRead(out var ev);
        Assert.NotNull(ev);
        Assert.Contains("chicken road", ev.Payload);
        Assert.Contains("resultCount", ev.Payload);
    }

    [Fact]
    public void Track_WithNullPayload_WritesNullPayload()
    {
        var channel = Channel.CreateUnbounded<UserEvent>();
        var tracker = new EventTrackerService(channel);

        tracker.Track(EventTypes.ProductView, productId: 10);

        channel.Reader.TryRead(out var ev);
        Assert.NotNull(ev);
        Assert.Null(ev.Payload);
    }

    [Fact]
    public void Track_SetsCorrectEventFields()
    {
        var channel   = Channel.CreateUnbounded<UserEvent>();
        var tracker   = new EventTrackerService(channel);
        var userId    = Guid.NewGuid();
        var sessionId = "sess-abc123";

        tracker.Track(
            EventTypes.CartAdd,
            productId: 42,
            userId:    userId,
            sessionId: sessionId);

        channel.Reader.TryRead(out var ev);
        Assert.Equal(EventTypes.CartAdd, ev!.EventType);
        Assert.Equal(42,        ev.ProductId);
        Assert.Equal(userId,    ev.UserId);
        Assert.Equal(sessionId, ev.SessionId);
    }

    // ── GetShopAnalyticsHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task GetShopAnalytics_ReturnsTotalsAndRows()
    {
        var eventsRepo = Substitute.For<IUserEventRepository>();
        eventsRepo.GetProductStatsForShopAsync(ShopId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductEventStats>
            {
                new(1, "ChickenRoad",  Views: 100, CartAdds: 30, Purchases: 10),
                new(2, "TwistXmas",    Views: 50,  CartAdds: 10, Purchases: 5),
            });

        var handler = new GetShopAnalyticsHandler(eventsRepo);
        var result  = await handler.Handle(new GetShopAnalyticsQuery(ShopId, Days: 30), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, TotalViews: {result.Data?.TotalViews}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        var dto = result.Data!;
        Assert.Equal(150, dto.TotalViews);
        Assert.Equal(40,  dto.TotalCartAdds);
        Assert.Equal(15,  dto.TotalPurchases);
        Assert.Equal(2,   dto.Products.Count);
        // Ordered descending by views
        Assert.Equal(1, dto.Products[0].ProductId);
        Assert.Equal(10m, dto.Products[0].ConversionRate);   // 10/100 * 100 = 10%
    }

    [Fact]
    public async Task GetShopAnalytics_NoData_ReturnsZeroTotals()
    {
        var eventsRepo = Substitute.For<IUserEventRepository>();
        eventsRepo.GetProductStatsForShopAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductEventStats>());

        var handler = new GetShopAnalyticsHandler(eventsRepo);
        var result  = await handler.Handle(new GetShopAnalyticsQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(0, result.Data!.TotalViews);
        Assert.Empty(result.Data.Products);
    }

    [Fact]
    public async Task GetShopAnalytics_RepositoryThrows_ReturnsError()
    {
        var eventsRepo = Substitute.For<IUserEventRepository>();
        eventsRepo.GetProductStatsForShopAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("DB error"));

        var handler = new GetShopAnalyticsHandler(eventsRepo);
        var result  = await handler.Handle(new GetShopAnalyticsQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB error", result.ErrorMessage);
    }

    [Fact]
    public async Task GetShopAnalytics_ZeroViews_ConversionRateIsZero()
    {
        var eventsRepo = Substitute.For<IUserEventRepository>();
        eventsRepo.GetProductStatsForShopAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductEventStats>
            {
                new(3, "NoViewsProduct", Views: 0, CartAdds: 0, Purchases: 0),
            });

        var handler = new GetShopAnalyticsHandler(eventsRepo);
        var result  = await handler.Handle(new GetShopAnalyticsQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(0m, result.Data!.Products[0].ConversionRate);
    }

    // ── GetProductFunnelHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task GetProductFunnel_ReturnsFunnelData()
    {
        var eventsRepo  = Substitute.For<IUserEventRepository>();
        var productRepo = Substitute.For<IProductRepository>();

        var product = new Product("ChickenRoad", "Slot assets", ShopId);
        productRepo.GetByIdAsync(5).Returns(Task.FromResult<Product?>(product));

        eventsRepo.GetCountsByTypeForProductAsync(5, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int>
            {
                [EventTypes.ProductView]      = 200,
                [EventTypes.CartAdd]          = 60,
                [EventTypes.ProductPurchased] = 20,
            });

        var handler = new GetProductFunnelHandler(eventsRepo, productRepo);
        var result  = await handler.Handle(new GetProductFunnelQuery(5, ShopId, 30), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Views: {result.Data?.Views}, Conversion: {result.Data?.ConversionRate}%");

        Assert.Equal(ResponseStatus.Success, result.Status);
        var dto = result.Data!;
        Assert.Equal(5,    dto.ProductId);
        Assert.Equal(200,  dto.Views);
        Assert.Equal(60,   dto.CartAdds);
        Assert.Equal(20,   dto.Purchases);
        Assert.Equal(10m,  dto.ConversionRate);   // 20/200 * 100 = 10%
    }

    [Fact]
    public async Task GetProductFunnel_ProductNotFound_ReturnsError()
    {
        var eventsRepo  = Substitute.For<IUserEventRepository>();
        var productRepo = Substitute.For<IProductRepository>();
        productRepo.GetByIdAsync(99).Returns(Task.FromResult<Product?>(null));

        var handler = new GetProductFunnelHandler(eventsRepo, productRepo);
        var result  = await handler.Handle(new GetProductFunnelQuery(99, ShopId, 30), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task GetProductFunnel_ProductBelongsToDifferentShop_ReturnsError()
    {
        var eventsRepo  = Substitute.For<IUserEventRepository>();
        var productRepo = Substitute.For<IProductRepository>();

        var otherShop = Guid.NewGuid();
        var product   = new Product("OtherShopProduct", "Desc", otherShop);
        productRepo.GetByIdAsync(7).Returns(Task.FromResult<Product?>(product));

        var handler = new GetProductFunnelHandler(eventsRepo, productRepo);
        var result  = await handler.Handle(new GetProductFunnelQuery(7, ShopId, 30), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task GetProductFunnel_NoEvents_ReturnsZeroes()
    {
        var eventsRepo  = Substitute.For<IUserEventRepository>();
        var productRepo = Substitute.For<IProductRepository>();

        var product = new Product("SilentProduct", "Desc", ShopId);
        productRepo.GetByIdAsync(8).Returns(Task.FromResult<Product?>(product));

        eventsRepo.GetCountsByTypeForProductAsync(8, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int>());

        var handler = new GetProductFunnelHandler(eventsRepo, productRepo);
        var result  = await handler.Handle(new GetProductFunnelQuery(8, ShopId, 7), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(0, result.Data!.Views);
        Assert.Equal(0, result.Data.Purchases);
        Assert.Equal(0m, result.Data.ConversionRate);
        Assert.Equal(7, result.Data.Days);
    }

    // ── GetAdminAnalyticsHandler ────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminAnalytics_ReturnsCountsAndTopSearches()
    {
        var eventsRepo = Substitute.For<IUserEventRepository>();
        var counts = new Dictionary<string, int>
        {
            [EventTypes.ProductView]  = 1000,
            [EventTypes.Search]       = 300,
            [EventTypes.CartAdd]      = 150,
        };
        var topSearches = new List<TopSearchEntry>
        {
            new("chicken road",  80),
            new("slot machine",  60),
        };

        eventsRepo.GetPlatformCountsByTypeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(counts);
        eventsRepo.GetTopSearchesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(topSearches);

        var handler = new GetAdminAnalyticsHandler(eventsRepo);
        var result  = await handler.Handle(new GetAdminAnalyticsQuery(Days: 30), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, EventCounts: {result.Data?.EventCounts.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        var dto = result.Data!;
        Assert.Equal(30,   dto.Days);
        Assert.Equal(1000, dto.EventCounts[EventTypes.ProductView]);
        Assert.Equal(2,    dto.TopSearches.Count);
        Assert.Equal("chicken road", dto.TopSearches[0].Query);
    }

    [Fact]
    public async Task GetAdminAnalytics_RepositoryThrows_ReturnsError()
    {
        var eventsRepo = Substitute.For<IUserEventRepository>();
        eventsRepo.GetPlatformCountsByTypeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Platform analytics unavailable"));

        var handler = new GetAdminAnalyticsHandler(eventsRepo);
        var result  = await handler.Handle(new GetAdminAnalyticsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Platform analytics unavailable", result.ErrorMessage);
    }
}

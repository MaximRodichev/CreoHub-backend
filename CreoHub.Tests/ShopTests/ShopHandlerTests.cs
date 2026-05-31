using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.Queries.Shop;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using AutoMapper;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ShopTests;

public class ShopHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly IShopRepository _shopRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IProductRepository _productRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ShopId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public ShopHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _shopRepo = Substitute.For<IShopRepository>();
        _accountRepo = Substitute.For<IAccountRepository>();
        _productRepo = Substitute.For<IProductRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _tagRepo = Substitute.For<ITagRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _mapper = Substitute.For<IMapper>();
    }

    // ── CreateShop ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateShop_IsCurrentlyLocked_ReturnsError()
    {
        // Handler is intentionally locked for controlled onboarding — returns error for all callers
        var user = User.Create("MaxG", "max@gmail.com");
        var dto = new CreateShopDTO { Name = "GamblElements", Description = "Slot assets shop" };

        var handler = new CreateShopHandler(_mapper, _unitOfWork, _shopRepo, _accountRepo);
        var result = await handler.Handle(new CreateShopCommand(user.Id, dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ShopId: {result.Data}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("временно недоступно", result.ErrorMessage);
        await _shopRepo.DidNotReceive().AddAsync(Arg.Any<Shop>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateShop_WhenLocked_ReturnsLockedError()
    {
        // Even if the user doesn't exist, the locked guard fires first
        var dto = new CreateShopDTO { Name = "Ghost Shop", Description = "N/A" };
        _accountRepo.GetByIdAsync(UserId).ReturnsNull();

        var handler = new CreateShopHandler(_mapper, _unitOfWork, _shopRepo, _accountRepo);
        var result = await handler.Handle(new CreateShopCommand(UserId, dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("временно недоступно", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateShop_RepositoryThrows_ReturnsError()
    {
        var dto = new CreateShopDTO { Name = "Err Shop", Description = "desc" };
        _shopRepo.AddAsync(Arg.Any<Shop>()).Throws(new Exception("DB error"));

        var handler = new CreateShopHandler(_mapper, _unitOfWork, _shopRepo, _accountRepo);
        var result = await handler.Handle(new CreateShopCommand(UserId, dto), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetShopsShortInfo ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetShopsShortInfo_ReturnsShopList()
    {
        var shops = new List<ShopShortInfoDTO>
        {
            new ShopShortInfoDTO { Id = ShopId, Name = "GamblElements" }
        };
        _shopRepo.GetShopsShortInfoAsync().Returns(Task.FromResult(shops));

        var handler = new GetShopsShortInfoHandler(_shopRepo, _unitOfWork);
        var result = await handler.Handle(new GetShopsShortInfoQuery(), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Single(result.Data);
        Assert.Equal("GamblElements", result.Data[0].Name);
    }

    [Fact]
    public async Task GetShopsShortInfo_RepositoryThrows_ReturnsError()
    {
        _shopRepo.GetShopsShortInfoAsync().Throws(new Exception("DB error"));

        var handler = new GetShopsShortInfoHandler(_shopRepo, _unitOfWork);
        var result = await handler.Handle(new GetShopsShortInfoQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetShopDashboard ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetShopDashboard_ReturnsAggregatedData()
    {
        var stats = new ShopStatsDTO(1500m, 10, 5, 8, new Dictionary<string, decimal> { { "2026-01", 1500m } });
        var topProducts = new List<ProductStatsDTO>();
        var recentDeals = new List<OrderShortInfoDTO>();
        var tagStats = new List<TagStatsDTO>();

        _productRepo.GetProductsStatsByShopIdAsync(ShopId, null, null, 5).Returns(Task.FromResult(topProducts));
        _shopRepo.GetShopStatsAsync(ShopId, null, null).Returns(Task.FromResult(stats));
        _orderRepo.GetOrdersShortInfoByShopIdAsync(ShopId, null, null, 5).Returns(Task.FromResult(recentDeals));
        _tagRepo.GetTagStatsByShopAsync(ShopId, null, null, 5).Returns(Task.FromResult(tagStats));

        var handler = new GetShopDashboardHandler(_tagRepo, _shopRepo, _orderRepo, _productRepo);
        var result = await handler.Handle(new GetShopDashboardQuery(ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Revenue: {result.Data?.Stats?.TotalRevenue}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(1500m, result.Data.Stats.TotalRevenue);
        Assert.Equal(10, result.Data.Stats.TotalOrders);
    }

    [Fact]
    public async Task GetShopDashboard_RepositoryThrows_ReturnsError()
    {
        _productRepo.GetProductsStatsByShopIdAsync(ShopId, null, null, 5)
            .Throws(new Exception("Timeout"));

        var handler = new GetShopDashboardHandler(_tagRepo, _shopRepo, _orderRepo, _productRepo);
        var result = await handler.Handle(new GetShopDashboardQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }
}

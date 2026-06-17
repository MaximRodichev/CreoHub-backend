using Creohub.Domain.Entities;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.Services;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.OrderTests;

/// <summary>
/// Tests for CompletePaymentHandler milestone promo-code logic
/// and for GetOrdersShortInfoByShopIdQuery date-filter passthrough.
/// </summary>
public class CompletePaymentMilestoneTests
{
    private readonly ITestOutputHelper _output;

    private readonly IUnitOfWork                      _unitOfWork;
    private readonly IUserTransactionRepository       _txRepo;
    private readonly IOrderRepository                 _orderRepo;
    private readonly IContentFileRepository           _contentFileRepo;
    private readonly IContentAccessRepository         _accessRepo;
    private readonly IProductRepository               _productRepo;
    private readonly IShopTransactionRepository       _shopTxRepo;
    private readonly IShopBalanceRepository           _shopBalanceRepo;
    private readonly IAccountRepository               _accountRepo;
    private readonly ISubscriptionPromoCodeRepository _promoRepo;
    private readonly IEventTracker _events;

    private static readonly Guid UserId = Guid.Parse("cccc0000-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ShopId = Guid.Parse("dddd0000-dddd-dddd-dddd-dddddddddddd");

    public CompletePaymentMilestoneTests(ITestOutputHelper output)
    {
        _output          = output;
        _unitOfWork      = Substitute.For<IUnitOfWork>();
        _txRepo          = Substitute.For<IUserTransactionRepository>();
        _orderRepo       = Substitute.For<IOrderRepository>();
        _contentFileRepo = Substitute.For<IContentFileRepository>();
        _accessRepo      = Substitute.For<IContentAccessRepository>();
        _productRepo     = Substitute.For<IProductRepository>();
        _shopTxRepo      = Substitute.For<IShopTransactionRepository>();
        _shopBalanceRepo = Substitute.For<IShopBalanceRepository>();
        _accountRepo     = Substitute.For<IAccountRepository>();
        _promoRepo       = Substitute.For<ISubscriptionPromoCodeRepository>();
        _events          = Substitute.For<IEventTracker>();

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _shopTxRepo.AddAsync(Arg.Any<ShopTransaction>())
            .Returns(ci => Task.FromResult(ci.Arg<ShopTransaction>()));
        _accessRepo.AddAsync(Arg.Any<ContentAccess>())
            .Returns(ci => Task.FromResult(ci.Arg<ContentAccess>()));
        _shopBalanceRepo.AddAsync(Arg.Any<ShopBalance>())
            .Returns(ci => Task.FromResult(ci.Arg<ShopBalance>()));
    }

    private CompletePaymentHandler BuildHandler() =>
        new(_unitOfWork, _txRepo, _orderRepo, _contentFileRepo, _accessRepo,
            _productRepo, _shopTxRepo, _shopBalanceRepo, _accountRepo, _promoRepo,
            Substitute.For<ICartRepository>(), _events, Substitute.For<INotificationService>());

    private (UserTransaction tx, Order order, Product product, User user) BuildScenario(
        decimal itemPrice, decimal userLifetimeSpent = 0m, string trackId = "track-123")
    {
        var user = User.Create("MaxG", "max@gmail.com");
        if (userLifetimeSpent > 0)
            user.AddSpend(userLifetimeSpent);

        var product = new Product("ChickenRoad Pack", "Casino assets", ShopId);
        product.AddPrice(itemPrice);

        // Use SubscriptionPurchase factory (no Order required) — we only need a valid Id
        var tx    = UserTransaction.CreateSubscriptionPurchase(itemPrice, user.Id, trackId);
        var order = Order.Open("",
            new List<(Product product, List<ContentFile> selectedFiles)>
            {
                (product, new List<ContentFile>())
            },
            user.Id);

        return (tx, order, product, user);
    }

    private void SetupHappyPath(
        UserTransaction tx, Order order, Product product, User user,
        string trackId = "track-123")
    {
        _txRepo.GetByTrackIdAsync(trackId)
            .Returns(Task.FromResult<UserTransaction?>(tx));
        _orderRepo.GetByTransactionIdWithItemsAsync(tx.Id)
            .Returns(Task.FromResult<Order?>(order));
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product> { product }));
        _shopBalanceRepo.GetByShopIdAsync(ShopId).ReturnsNull();
        _contentFileRepo.GetByProductIdAsync(Arg.Any<int>())
            .Returns(Task.FromResult(new List<CreoHub.Domain.Entities.ContentFile>()));
        _accountRepo.GetByIdAsync(order.CustomerId)
            .Returns(Task.FromResult<User?>(user));
    }

    // ── Milestone promo code creation ─────────────────────────────────────────

    [Fact]
    public async Task CompletePayment_CrossesMilestone500_CreatesPromoCode()
    {
        // User was at $450, order is $100 → crosses $500
        var (tx, order, product, user) = BuildScenario(100m, 450m);
        SetupHappyPath(tx, order, product, user);

        _promoRepo.WasMilestoneIssuedAsync(user.Id, "lifetime_500") .Returns(Task.FromResult(false));
        _promoRepo.WasMilestoneIssuedAsync(user.Id, "lifetime_1000").Returns(Task.FromResult(false));
        _promoRepo.WasMilestoneIssuedAsync(user.Id, "lifetime_2500").Returns(Task.FromResult(false));
        _promoRepo.AddAsync(Arg.Any<SubscriptionPromoCode>()).Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(
            new CompletePaymentCommand("track-123", "0xABC", "0xSENDER"),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        await _promoRepo.Received(1).AddAsync(Arg.Is<SubscriptionPromoCode>(
            p => p.MilestoneTag == "lifetime_500" && p.Days == 90));
    }

    [Fact]
    public async Task CompletePayment_MilestoneAlreadyIssued_DoesNotDuplicate()
    {
        var (tx, order, product, user) = BuildScenario(100m, 450m);
        SetupHappyPath(tx, order, product, user);

        // lifetime_500 already issued
        _promoRepo.WasMilestoneIssuedAsync(user.Id, "lifetime_500") .Returns(Task.FromResult(true));
        _promoRepo.WasMilestoneIssuedAsync(user.Id, "lifetime_1000").Returns(Task.FromResult(false));
        _promoRepo.WasMilestoneIssuedAsync(user.Id, "lifetime_2500").Returns(Task.FromResult(false));

        var result = await BuildHandler().Handle(
            new CompletePaymentCommand("track-123", "0xABC", "0xSENDER"),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await _promoRepo.DidNotReceive().AddAsync(Arg.Is<SubscriptionPromoCode>(
            p => p.MilestoneTag == "lifetime_500"));
    }

    [Fact]
    public async Task CompletePayment_BelowAllMilestones_NoPromoCreated()
    {
        // $10 order, $0 before → stays at $10, below all milestones
        var (tx, order, product, user) = BuildScenario(10m, 0m);
        SetupHappyPath(tx, order, product, user);

        _promoRepo.WasMilestoneIssuedAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var result = await BuildHandler().Handle(
            new CompletePaymentCommand("track-123", "0xABC", "0xSENDER"),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await _promoRepo.DidNotReceive().AddAsync(Arg.Any<SubscriptionPromoCode>());
    }

    [Fact]
    public async Task CompletePayment_CrossesAllMilestones_CreatesThreeCodes()
    {
        // $3000 order, $0 before → crosses all three milestones
        var (tx, order, product, user) = BuildScenario(3000m, 0m);
        SetupHappyPath(tx, order, product, user);

        _promoRepo.WasMilestoneIssuedAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));
        _promoRepo.AddAsync(Arg.Any<SubscriptionPromoCode>()).Returns(Task.CompletedTask);

        var result = await BuildHandler().Handle(
            new CompletePaymentCommand("track-123", "0xABC", "0xSENDER"),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await _promoRepo.Received(1).AddAsync(Arg.Is<SubscriptionPromoCode>(p => p.MilestoneTag == "lifetime_500"));
        await _promoRepo.Received(1).AddAsync(Arg.Is<SubscriptionPromoCode>(p => p.MilestoneTag == "lifetime_1000"));
        await _promoRepo.Received(1).AddAsync(Arg.Is<SubscriptionPromoCode>(p => p.MilestoneTag == "lifetime_2500"));
    }

    [Fact]
    public async Task CompletePayment_MilestoneCodes_AreForAutoSlot()
    {
        var (tx, order, product, user) = BuildScenario(100m, 450m);
        SetupHappyPath(tx, order, product, user);

        _promoRepo.WasMilestoneIssuedAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));
        _promoRepo.AddAsync(Arg.Any<SubscriptionPromoCode>()).Returns(Task.CompletedTask);

        await BuildHandler().Handle(
            new CompletePaymentCommand("track-123", "0xABC", "0xSENDER"),
            CancellationToken.None);

        await _promoRepo.Received(1).AddAsync(Arg.Is<SubscriptionPromoCode>(
            p => p.ProductType == SubscriptionProductType.AutoSlot));
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CompletePayment_TransactionNotFound_ReturnsError()
    {
        _txRepo.GetByTrackIdAsync("track-missing").ReturnsNull();

        var result = await BuildHandler().Handle(
            new CompletePaymentCommand("track-missing", "0xABC", "0xSENDER"),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Не удалось обработать", result.ErrorMessage);
    }

    [Fact]
    public async Task CompletePayment_OrderNotFound_ReturnsError()
    {
        var user = User.Create("MaxG", "max@gmail.com");
        var tx   = UserTransaction.CreateSubscriptionPurchase(50m, user.Id, "track-orphan");

        _txRepo.GetByTrackIdAsync("track-orphan").Returns(Task.FromResult<UserTransaction?>(tx));
        _orderRepo.GetByTransactionIdWithItemsAsync(tx.Id).ReturnsNull();

        var result = await BuildHandler().Handle(
            new CompletePaymentCommand("track-orphan", "0xABC", "0xSENDER"),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Не удалось обработать", result.ErrorMessage);
    }

    // ── GetOrdersShortInfoByShopIdQuery — date filter passthrough ─────────────

    [Fact]
    public async Task GetOrdersShortInfo_WithDateRange_PassesDatesToRepository()
    {
        var from  = new DateTime(2025, 1,  1, 0, 0, 0, DateTimeKind.Utc);
        var to    = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var repo  = Substitute.For<IOrderRepository>();
        repo.GetOrdersShortInfoByShopIdAsync(ShopId, from, to, Arg.Any<int?>())
            .Returns(Task.FromResult(new List<OrderShortInfoDTO>()));

        var result = await new GetOrdersShortInfoByShopIdHandler(repo).Handle(
            new GetOrdersShortInfoByShopIdQuery(ShopId, from, to),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await repo.Received(1)
            .GetOrdersShortInfoByShopIdAsync(ShopId, from, to, Arg.Any<int?>());
    }

    [Fact]
    public async Task GetOrdersShortInfo_NoDates_PassesNullToRepository()
    {
        var repo = Substitute.For<IOrderRepository>();
        repo.GetOrdersShortInfoByShopIdAsync(ShopId, null, null, Arg.Any<int?>())
            .Returns(Task.FromResult(new List<OrderShortInfoDTO>()));

        await new GetOrdersShortInfoByShopIdHandler(repo).Handle(
            new GetOrdersShortInfoByShopIdQuery(ShopId),
            CancellationToken.None);

        await repo.Received(1)
            .GetOrdersShortInfoByShopIdAsync(ShopId, null, null, Arg.Any<int?>());
    }
}

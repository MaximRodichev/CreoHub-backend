using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Services;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Pricing;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Repositories;
using Microsoft.Extensions.Options;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;

using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.BalanceTests;

/// <summary>
/// Тесты CheckoutWithBalanceHandler — мгновенная оплата с баланса без OxaPay.
/// </summary>
public class CheckoutWithBalanceHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IUnitOfWork              _unitOfWork;
    private readonly IOrderRepository         _orderRepo;
    private readonly IProductRepository       _productRepo;
    private readonly IUserTransactionRepository _transactionRepo;
    private readonly IUserBalanceRepository   _balanceRepo;
    private readonly IContentFileRepository   _contentFileRepo;
    private readonly IContentAccessRepository _accessRepo;

    private static readonly Guid UserId   = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const           int  ProductId = 10;

    public CheckoutWithBalanceHandlerTests(ITestOutputHelper output)
    {
        _output          = output;
        _unitOfWork      = Substitute.For<IUnitOfWork>();
        _orderRepo       = Substitute.For<IOrderRepository>();
        _productRepo     = Substitute.For<IProductRepository>();
        _transactionRepo = Substitute.For<IUserTransactionRepository>();
        _balanceRepo     = Substitute.For<IUserBalanceRepository>();
        _contentFileRepo = Substitute.For<IContentFileRepository>();
        _accessRepo      = Substitute.For<IContentAccessRepository>();
    }

    private CheckoutWithBalanceHandler MakeHandler() =>
        new(_unitOfWork, _orderRepo, _productRepo, _transactionRepo,
            _balanceRepo, _contentFileRepo, _accessRepo,
            Substitute.For<ICartRepository>(),
            Substitute.For<IShopTransactionRepository>(),
            Substitute.For<IShopBalanceRepository>(),
            Substitute.For<IAccountRepository>(),
            Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }),
            Substitute.For<IEventTracker>());

    /// <summary>Создаёт Product с ценой (через рефлексию не нужно — используем публичный API).</summary>
    private static Product MakeProductWithPrice(decimal price)
    {
        var product = new Product("Test Product", "Test Desc", Guid.NewGuid());
        product.AddPrice(price);
        return product;
    }

    // ── Успешный сценарий ─────────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutWithBalance_SufficientBalance_CompletesOrder()
    {
        var product = MakeProductWithPrice(50m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());
        _accessRepo.GetByUserIdAsync(UserId).Returns(new List<ContentAccess>());
        // accountRepo returns null → no lifetime discount; 1 item → count 0% → buyerPays = 50

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        var handler = MakeHandler();
        var result  = await handler.Handle(new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, OrderId: {result.Data?.OrderId}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotEqual(Guid.Empty, result.Data!.OrderId);
        Assert.Equal(string.Empty, result.Data.PaymentUrl);
        // 1 item = count discount 0%, no lifetime → buyerPays = 50 → balance = 100 - 50 = 50
        Assert.Equal(50m, balance.AvailableAmount);
        // SaveChanges вызывается 2 раза: единый атомарный коммит (Order+баланс+доступы)
        // + коммит очистки корзины (try/catch). LifetimeSpent пропускается т.к. user == null.
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Недостаточный баланс ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutWithBalance_InsufficientBalance_ReturnsError()
    {
        var product = MakeProductWithPrice(200m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(50m);

        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _accessRepo.GetByUserIdAsync(UserId).Returns(new List<ContentAccess>());

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        var handler = MakeHandler();
        var result  = await handler.Handle(new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        _output.WriteLine($"Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Insufficient balance", result.ErrorMessage);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Баланс не найден ──────────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutWithBalance_NoBalance_ReturnsError()
    {
        _balanceRepo.GetByUserIdAsync(UserId).ReturnsNull();

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = ProductId, FileIds = new List<Guid>() }
        };

        var handler = MakeHandler();
        var result  = await handler.Handle(new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Insufficient balance", result.ErrorMessage);
    }

    // ── Пустой список товаров ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutWithBalance_EmptyItems_ReturnsError()
    {
        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CheckoutWithBalanceCommand(UserId, new List<CheckoutItemDTO>()), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("empty", result.ErrorMessage);
    }

    // ── Null список товаров ────────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutWithBalance_NullItems_ReturnsError()
    {
        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CheckoutWithBalanceCommand(UserId, null!), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── Продукт не найден ─────────────────────────────────────────────────────

    [Fact]
    public async Task CheckoutWithBalance_ProductNotFound_ReturnsError()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product>());

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = 999, FileIds = new List<Guid>() }
        };

        var handler = MakeHandler();
        var result  = await handler.Handle(new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage);
    }
}

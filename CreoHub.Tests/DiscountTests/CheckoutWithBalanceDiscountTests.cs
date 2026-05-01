using System.Reflection;
using System.Runtime.CompilerServices;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.DiscountTests;

/// <summary>
/// Тесты CheckoutWithBalance — скидки (F1+F2) и бандл-сценарии.
/// </summary>
public class CheckoutWithBalanceDiscountTests
{
    private readonly ITestOutputHelper _output;

    private readonly IUnitOfWork                _unitOfWork;
    private readonly IOrderRepository           _orderRepo;
    private readonly IProductRepository         _productRepo;
    private readonly IUserTransactionRepository _transactionRepo;
    private readonly IUserBalanceRepository     _balanceRepo;
    private readonly IContentFileRepository     _contentFileRepo;
    private readonly IContentAccessRepository   _accessRepo;
    private readonly ICartRepository            _cartRepo;
    private readonly IShopTransactionRepository _shopTxRepo;
    private readonly IShopBalanceRepository     _shopBalanceRepo;
    private readonly IAccountRepository         _accountRepo;

    private static readonly Guid UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public CheckoutWithBalanceDiscountTests(ITestOutputHelper output)
    {
        _output          = output;
        _unitOfWork      = Substitute.For<IUnitOfWork>();
        _orderRepo       = Substitute.For<IOrderRepository>();
        _productRepo     = Substitute.For<IProductRepository>();
        _transactionRepo = Substitute.For<IUserTransactionRepository>();
        _balanceRepo     = Substitute.For<IUserBalanceRepository>();
        _contentFileRepo = Substitute.For<IContentFileRepository>();
        _accessRepo      = Substitute.For<IContentAccessRepository>();
        _cartRepo        = Substitute.For<ICartRepository>();
        _shopTxRepo      = Substitute.For<IShopTransactionRepository>();
        _shopBalanceRepo = Substitute.For<IShopBalanceRepository>();
        _accountRepo     = Substitute.For<IAccountRepository>();
    }

    private CheckoutWithBalanceHandler MakeHandler() =>
        new(_unitOfWork, _orderRepo, _productRepo, _transactionRepo,
            _balanceRepo, _contentFileRepo, _accessRepo, _cartRepo,
            _shopTxRepo, _shopBalanceRepo, _accountRepo);

    /// <summary>Мокает общие зависимости, которые нужны для прохождения через handler.</summary>
    private void SetupDefaultMocks()
    {
        _accessRepo.GetByUserIdAsync(UserId).Returns(new List<ContentAccess>());
        _shopBalanceRepo.GetByShopIdAsync(Arg.Any<Guid>()).Returns((ShopBalance?)null);
        // GetByProductIdsAsync нужен для бандл-пути; по умолчанию — пустой список
        _contentFileRepo.GetByProductIdsAsync(Arg.Any<IEnumerable<int>>())
                         .Returns(new List<ContentFile>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Product MakeProduct(decimal price)
    {
        var p = new Product("P", "D", Guid.NewGuid());
        p.AddPrice(price);
        return p;
    }

    private static User MakeUser(decimal lifetimeSpent = 0m)
    {
        var u = User.Create("Test", "t@t.com");
        if (lifetimeSpent > 0) u.AddSpend(lifetimeSpent);
        return u;
    }

    /// <summary>
    /// Создаёт ContentFile через RuntimeHelpers (обходит приватный конструктор).
    /// </summary>
    private static ContentFile MakeContentFile(Guid id, int productId)
    {
        var cf = (ContentFile)RuntimeHelpers.GetUninitializedObject(typeof(ContentFile));
        SetProp(cf, "Id", id);
        SetProp(cf, "ProductId", productId);
        return cf;
    }

    /// <summary>
    /// Создаёт бандл-продукт с нужными BundleItems через рефлексию backing field.
    /// child.Id должен отличаться от bundleId (ProductBundle проверяет это).
    /// Используем RuntimeHelpers чтобы обойти конструктор.
    /// </summary>
    private static Product MakeBundleProduct(decimal price, List<(int childId, Guid childShopId)> childInfos)
    {
        // Создаём бандл-продукт через RuntimeHelpers, чтобы избежать проверки bundleId == productId
        var bundle = (Product)RuntimeHelpers.GetUninitializedObject(typeof(Product));
        SetProp(bundle, "Id", 999);  // уникальный Id для бандла
        SetProp(bundle, "Name", "Bundle");
        SetProp(bundle, "Description", "D");
        SetProp(bundle, "OwnerId", childInfos.Count > 0 ? childInfos[0].childShopId : Guid.NewGuid());
        SetProp(bundle, "ProductType", ProductType.Bundle);
        SetProp(bundle, "ProductStatus", ProductStatus.Active);
        SetProp(bundle, "CreatedAt", DateTime.UtcNow);

        // Инициализируем backing fields через рефлексию
        InitBackingField<List<ProductBundle>>(bundle, "_bundleItems");
        InitBackingField<List<Price>>(bundle, "_prices");
        InitBackingField<List<ContentFile>>(bundle, "_contentFiles");
        InitBackingField<List<MediaProduct>>(bundle, "_mediaProducts");
        InitBackingField<List<OrderItem>>(bundle, "_orderItems");
        InitBackingField<List<Tag>>(bundle, "_tags");

        // Добавляем Price (поля: Value, Date, ProductId)
        var priceObj = (Price)RuntimeHelpers.GetUninitializedObject(typeof(Price));
        SetProp(priceObj, "Value", price);
        SetProp(priceObj, "Date", DateTime.UtcNow);
        SetProp(priceObj, "ProductId", 999);
        GetBackingField<List<Price>>(bundle, "_prices").Add(priceObj);

        // Добавляем BundleItems
        var bundleItemsList = GetBackingField<List<ProductBundle>>(bundle, "_bundleItems");
        foreach (var (childId, _) in childInfos)
        {
            var pb = (ProductBundle)RuntimeHelpers.GetUninitializedObject(typeof(ProductBundle));
            SetProp(pb, "BundleId", 999);
            SetProp(pb, "ProductId", childId);
            bundleItemsList.Add(pb);
        }

        return bundle;
    }

    private static void SetProp(object obj, string propName, object? value)
    {
        var prop = obj.GetType().GetProperty(propName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    private static void InitBackingField<T>(object obj, string fieldName) where T : new()
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, new T());
    }

    private static T GetBackingField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)field!.GetValue(obj)!;
    }

    // ── F2: cart volume discount применяется ─────────────────────────────────

    [Fact]
    public async Task Checkout_CartTotal50_Applies3PercentDiscount()
    {
        // product price = $50 → cart volume 3% → buyerPays = $48.50
        var product = MakeProduct(50m);
        var user    = MakeUser(0m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        _output.WriteLine($"Status={result.Status}, Error={result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Success, result.Status);

        // 50 * (1 - 0.03) = 48.50
        var expectedPays = 50m * (1m - 0.03m);
        Assert.Equal(expectedPays, 100m - balance.AvailableAmount);
    }

    // ── F1: lifetime discount применяется ────────────────────────────────────

    [Fact]
    public async Task Checkout_LifetimeSpent1000_Applies6PercentDiscount()
    {
        // product price = $30, lifetime 6%, cart 0% (< $50)
        // buyerPays = 30 * 0.94 = 28.20
        var product = MakeProduct(30m);
        var user    = MakeUser(1000m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);

        var expectedPays = 30m * (1m - 0.06m);   // 28.20
        Assert.Equal(expectedPays, 100m - balance.AvailableAmount);
    }

    // ── F1 + F2 складываются ─────────────────────────────────────────────────

    [Fact]
    public async Task Checkout_Lifetime6_Cart6_TotalDiscount12Percent()
    {
        // product price = $120: lifetime 6% (≥1000) + cart 6% ($100-$199) = 12%
        // buyerPays = 120 * 0.88 = 105.60
        var product = MakeProduct(120m);
        var user    = MakeUser(1000m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(200m);

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);

        var expectedPays = 120m * (1m - 0.12m);   // 105.60
        Assert.Equal(expectedPays, 200m - balance.AvailableAmount);
        _output.WriteLine($"Balance after: {balance.AvailableAmount}, expected pays: {expectedPays}");
    }

    // ── AddSpend использует rawTotal, а не discounted buyerPays ─────────────

    [Fact]
    public async Task Checkout_AddSpend_UsesRawTotal_NotDiscountedAmount()
    {
        // product $50, cart 3% → buyerPays=48.50, но LifetimeSpent += 50 (rawTotal)
        var product = MakeProduct(50m);
        var user    = MakeUser(0m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        // AddSpend вызывается с rawTotal = 50, не с buyerPays = 48.50
        Assert.Equal(50m, user.LifetimeSpent);
    }

    // ── Бандл: ContentAccess выдаётся для всех child-файлов ─────────────────

    [Fact]
    public async Task Checkout_Bundle_GrantsContentAccessToAllChildFiles()
    {
        var shopId  = Guid.NewGuid();
        var child1Id = 101;
        var child2Id = 102;
        var child1FileId = Guid.NewGuid();
        var child2FileId = Guid.NewGuid();

        var bundle = MakeBundleProduct(40m, new List<(int, Guid)>
        {
            (child1Id, shopId),
            (child2Id, shopId),
        });

        var user    = MakeUser(0m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { bundle });
        _contentFileRepo.GetByProductIdAsync(child1Id)
            .Returns(new List<ContentFile> { MakeContentFile(child1FileId, child1Id) });
        _contentFileRepo.GetByProductIdAsync(child2Id)
            .Returns(new List<ContentFile> { MakeContentFile(child2FileId, child2Id) });
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = bundle.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        _output.WriteLine($"Status={result.Status}, Error={result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Success, result.Status);

        // Два вызова AddAsync — по одному на каждый child-файл
        await _accessRepo.Received(2).AddAsync(Arg.Any<ContentAccess>());
    }

    // ── Бандл без детей: нет ложной ошибки "все файлы куплены" ──────────────

    [Fact]
    public async Task Checkout_Bundle_EmptyBundleItems_NoFalseAlreadyOwnedError()
    {
        var bundle  = MakeBundleProduct(40m, new List<(int, Guid)>());  // нет детей

        var user    = MakeUser(0m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { bundle });
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = bundle.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.DoesNotContain("куплены", result.ErrorMessage ?? string.Empty);
    }

    // ── Бандл: повторная покупка (все файлы уже куплены) → ошибка ──────────

    [Fact]
    public async Task Checkout_Bundle_AllChildFilesAlreadyOwned_ReturnsError()
    {
        var shopId   = Guid.NewGuid();
        var child1Id = 101;
        var child2Id = 102;
        var file1Id  = Guid.NewGuid();
        var file2Id  = Guid.NewGuid();

        var bundle = MakeBundleProduct(40m, new List<(int, Guid)>
        {
            (child1Id, shopId),
            (child2Id, shopId),
        });

        var user    = MakeUser(0m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        // Пользователь уже владеет обоими файлами
        var ownedAccesses = new List<ContentAccess>
        {
            new ContentAccess(UserId, file1Id, Guid.NewGuid()),
            new ContentAccess(UserId, file2Id, Guid.NewGuid()),
        };

        var file1 = MakeContentFile(file1Id, child1Id);
        var file2 = MakeContentFile(file2Id, child2Id);

        _accessRepo.GetByUserIdAsync(UserId).Returns(ownedAccesses);
        _shopBalanceRepo.GetByShopIdAsync(Arg.Any<Guid>()).Returns((ShopBalance?)null);
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { bundle });
        _contentFileRepo.GetByProductIdAsync(child1Id).Returns(new List<ContentFile> { file1 });
        _contentFileRepo.GetByProductIdAsync(child2Id).Returns(new List<ContentFile> { file2 });
        // Batch-запрос для бандл-пути в CheckoutWithBalance
        _contentFileRepo.GetByProductIdsAsync(Arg.Any<IEnumerable<int>>())
                         .Returns(new List<ContentFile> { file1, file2 });
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = bundle.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        _output.WriteLine($"Status={result.Status}, Error={result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("уже приобрели", result.ErrorMessage);
    }

    // ── Недостаточно баланса с учётом скидки ────────────────────────────────

    [Fact]
    public async Task Checkout_InsufficientBalance_AfterDiscount_ReturnsError()
    {
        // product $50, cart 3%, buyerPays = 48.50, balance = 48.00 → fail
        var product = MakeProduct(50m);
        var user    = MakeUser(0m);
        var balance = new UserBalance(UserId);
        balance.AddFunds(48m);   // меньше 48.50

        SetupDefaultMocks();
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        _contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());
        _accountRepo.GetFullInfoByIdAsync(UserId).Returns(user);

        var items = new List<CheckoutItemDTO>
        {
            new() { ProductId = product.Id, FileIds = new List<Guid>() }
        };

        var result = await MakeHandler().Handle(
            new CheckoutWithBalanceCommand(UserId, items), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Insufficient balance", result.ErrorMessage);
    }
}

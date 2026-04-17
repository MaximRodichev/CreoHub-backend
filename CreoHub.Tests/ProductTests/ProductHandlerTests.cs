using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Queries.Product;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ProductTests;

public class ProductHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IProductRepository _productRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IShopRepository _shopRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IPriceRepository _priceRepo;
    private readonly IStorageObjectRepository _storageRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid ShopId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const int ProductId = 7;

    public ProductHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _productRepo = Substitute.For<IProductRepository>();
        _accountRepo = Substitute.For<IAccountRepository>();
        _shopRepo = Substitute.For<IShopRepository>();
        _tagRepo = Substitute.For<ITagRepository>();
        _priceRepo = Substitute.For<IPriceRepository>();
        _storageRepo = Substitute.For<IStorageObjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    // ── CreateProduct ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_ValidData_ReturnsSuccess()
    {
        var shop = new Shop("GamblElements", "Slot assets", UserId);
        var tags = new List<Tag> { new Tag("pragmatic play"), new Tag("netent") };
        var dto = new CreateProductDTO
        {
            Name = "InOut ChickenRoad",
            Description = "Full animation pack",
            Price = 25m,
            Tags = new List<string> { "pragmatic play", "netent" }
        };

        _shopRepo.GetByOwnerIdAsync(UserId).Returns(Task.FromResult(shop));
        _tagRepo.GetByNamesAsync(dto.Tags).Returns(Task.FromResult(tags));
        _productRepo.AddAsync(Arg.Any<Product>()).Returns(ci => Task.FromResult(ci.Arg<Product>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new CreateProductHandler(_productRepo, _accountRepo, _shopRepo, _tagRepo, _unitOfWork);
        var result = await handler.Handle(new CreateProductCommand(UserId, dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _productRepo.Received(1).AddAsync(Arg.Any<Product>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_ShopNotFound_ReturnsError()
    {
        _shopRepo.GetByOwnerIdAsync(UserId).Throws(new Exception("Shop not found for user"));

        var handler = new CreateProductHandler(_productRepo, _accountRepo, _shopRepo, _tagRepo, _unitOfWork);
        var result = await handler.Handle(
            new CreateProductCommand(UserId, new CreateProductDTO
            {
                Name = "X", Description = "Y", Price = 10m, Tags = new List<string>()
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Shop not found", result.ErrorMessage);
    }

    // ── ChangePrice ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePrice_SameShopOwner_ReturnsSuccess()
    {
        _accountRepo.GetShopByUserId(UserId).Returns(Task.FromResult(ShopId));
        _productRepo.GetShopIdByProductId(ProductId).Returns(Task.FromResult(ShopId));
        _priceRepo.AddAsync(Arg.Any<Price>()).Returns(ci => Task.FromResult(ci.Arg<Price>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new ChangePriceHandler(_priceRepo, _accountRepo, _unitOfWork, _productRepo);
        var result = await handler.Handle(
            new ChangePriceCommand(UserId, new ChangePriceDTO { ProductId = ProductId, Price = 30m }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        await _priceRepo.Received(1).AddAsync(Arg.Any<Price>());
    }

    [Fact]
    public async Task ChangePrice_DifferentShop_ReturnsAccessDenied()
    {
        var otherShopId = Guid.NewGuid();
        _accountRepo.GetShopByUserId(UserId).Returns(Task.FromResult(ShopId));
        _productRepo.GetShopIdByProductId(ProductId).Returns(Task.FromResult(otherShopId));

        var handler = new ChangePriceHandler(_priceRepo, _accountRepo, _unitOfWork, _productRepo);
        var result = await handler.Handle(
            new ChangePriceCommand(UserId, new ChangePriceDTO { ProductId = ProductId, Price = 30m }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Access denied", result.ErrorMessage);
        await _priceRepo.DidNotReceive().AddAsync(Arg.Any<Price>());
    }

    [Fact]
    public async Task ChangePrice_RepositoryThrows_ReturnsError()
    {
        _accountRepo.GetShopByUserId(UserId).Throws(new Exception("DB error"));

        var handler = new ChangePriceHandler(_priceRepo, _accountRepo, _unitOfWork, _productRepo);
        var result = await handler.Handle(
            new ChangePriceCommand(UserId, new ChangePriceDTO { ProductId = ProductId, Price = 30m }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── UpdateProduct ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_ProductNotFound_ReturnsFail()
    {
        _productRepo.GetProductById(ProductId).Returns(Task.FromResult<Product>(null!));

        var handler = new UpdateProductHandler(_productRepo, _unitOfWork, _tagRepo, _priceRepo, _storageRepo);
        var result = await handler.Handle(
            new UpdateProductCommand(ShopId, new UpdateProductInfoDTO
            {
                Id = ProductId, Name = "X", Description = "Y", Price = 10m,
                Tags = new List<string>(), ObjectStorageIds = null
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateProduct_NameChanged_UpdatesAndSaves()
    {
        var product = new Product("Old Name", "Old Desc", ShopId, null);
        product.AddPrice(20m);
        var newTags = new List<Tag> { new Tag("netent") };

        _productRepo.GetProductById(ProductId).Returns(Task.FromResult<Product>(product));
        _tagRepo.GetByNamesAsync(Arg.Any<List<string>>()).Returns(Task.FromResult(newTags));
        _priceRepo.AddAsync(Arg.Any<Price>()).Returns(ci => Task.FromResult(ci.Arg<Price>()));
        _productRepo.Update(Arg.Any<Product>()).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new UpdateProductHandler(_productRepo, _unitOfWork, _tagRepo, _priceRepo, _storageRepo);
        var result = await handler.Handle(
            new UpdateProductCommand(ShopId, new UpdateProductInfoDTO
            {
                Id = ProductId,
                Name = "New Name",
                Description = "Old Desc",
                Price = 20m,       // same price, no new Price record
                Tags = new List<string> { "netent" },
                ObjectStorageIds = null
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("New Name", product.Name);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_PriceChanged_AddsNewPriceRecord()
    {
        var product = new Product("Product A", "Desc", ShopId, null);
        product.AddPrice(20m);

        _productRepo.GetProductById(ProductId).Returns(Task.FromResult<Product>(product));
        _tagRepo.GetByNamesAsync(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<Tag>()));
        _priceRepo.AddAsync(Arg.Any<Price>()).Returns(ci => Task.FromResult(ci.Arg<Price>()));
        _productRepo.Update(Arg.Any<Product>()).Returns(product);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new UpdateProductHandler(_productRepo, _unitOfWork, _tagRepo, _priceRepo, _storageRepo);
        var result = await handler.Handle(
            new UpdateProductCommand(ShopId, new UpdateProductInfoDTO
            {
                Id = ProductId, Name = "Product A", Description = "Desc",
                Price = 35m,  // price changed
                Tags = new List<string>(), ObjectStorageIds = null
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await _priceRepo.Received(1).AddAsync(Arg.Any<Price>());
    }

    // ── GetProductsByFilter ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProductsByFilter_ReturnsPageView()
    {
        var products = new List<ProductViewDTO>
        {
            new ProductViewDTO { Id = 1, Name = "ChickenRoad", Price = 25m },
            new ProductViewDTO { Id = 2, Name = "TwistXmas",   Price = 12m },
        };
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>())
            .Returns(Task.FromResult<(List<ProductViewDTO>, int)>((products, 2)));

        var handler = new GetProductsByFilterHandler(_productRepo, null!);
        var result = await handler.Handle(
            new GetProductsByFilterQuery(new FiltersDto { Page = 1, PageSize = 10 }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.CountProducts}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data.CountProducts);
        Assert.Equal(2, result.Data.Products.Count);
    }

    [Fact]
    public async Task GetProductsByFilter_RepositoryThrows_ReturnsError()
    {
        _productRepo.GetProductsByFilters(Arg.Any<FiltersDto>()).Throws(new Exception("Query error"));

        var handler = new GetProductsByFilterHandler(_productRepo, null!);
        var result = await handler.Handle(
            new GetProductsByFilterQuery(new FiltersDto()),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetProductInfoByName ──────────────────────────────────────────────────

    [Fact]
    public async Task GetProductInfoByName_ReturnsProductInfo()
    {
        var dto = new ProductInfoDTO { Id = 1, Name = "ChickenRoad", Price = 25m };
        _productRepo.GetProductByName("ChickenRoad").Returns(Task.FromResult(dto));

        var handler = new GetProductInfoByNameHandler(_productRepo, null!);
        var result = await handler.Handle(new GetProductInfoByNameQuery("ChickenRoad"), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Name: {result.Data?.Name}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("ChickenRoad", result.Data.Name);
    }

    [Fact]
    public async Task GetProductInfoByName_NotFound_ReturnsError()
    {
        _productRepo.GetProductByName("Unknown").Throws(new Exception("Product not found"));

        var handler = new GetProductInfoByNameHandler(_productRepo, null!);
        var result = await handler.Handle(new GetProductInfoByNameQuery("Unknown"), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetProductAnalytics ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProductAnalytics_CorrectShop_ReturnsAnalytics()
    {
        var analytics = new ProductAnalyticsDTO
        {
            ShopId = ShopId,
            ProductId = ProductId,
            CountSells = 5,
            PriceHistory = new Dictionary<DateTime, decimal>(),
            ContentFileInfos = new List<ContentFileInfo>(),
            SellsHistory = new List<OrderSellDTO>(),
            SellsDateTimes = new List<DateTime>(),
            MediaViews = new List<StorageObjectViewDTO>()
        };
        _productRepo.GetProductAnalyticsById(ProductId).Returns(Task.FromResult(analytics));

        var handler = new GetProductAnalyticsHandler(_productRepo, null!);
        var result = await handler.Handle(
            new GetProductAnalyticsQuery(ShopId, ProductId),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Sells: {result.Data?.CountSells}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(5, result.Data.CountSells);
    }

    [Fact]
    public async Task GetProductAnalytics_WrongShop_ReturnsError()
    {
        var wrongShopId = Guid.NewGuid();
        var analytics = new ProductAnalyticsDTO
        {
            ShopId = ShopId,          // belongs to ShopId
            ProductId = ProductId,
            PriceHistory = new Dictionary<DateTime, decimal>(),
            ContentFileInfos = new List<ContentFileInfo>(),
            SellsHistory = new List<OrderSellDTO>(),
            SellsDateTimes = new List<DateTime>(),
            MediaViews = new List<StorageObjectViewDTO>()
        };
        _productRepo.GetProductAnalyticsById(ProductId).Returns(Task.FromResult(analytics));

        var handler = new GetProductAnalyticsHandler(_productRepo, null!);
        var result = await handler.Handle(
            new GetProductAnalyticsQuery(wrongShopId, ProductId),   // different shop
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains(wrongShopId.ToString(), result.ErrorMessage);
    }

    [Fact]
    public async Task GetProductAnalytics_RepositoryThrows_ReturnsError()
    {
        _productRepo.GetProductAnalyticsById(ProductId).Throws(new Exception("DB error"));

        var handler = new GetProductAnalyticsHandler(_productRepo, null!);
        var result = await handler.Handle(
            new GetProductAnalyticsQuery(ShopId, ProductId),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }
}

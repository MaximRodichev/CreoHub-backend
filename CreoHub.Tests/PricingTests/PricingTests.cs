using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Pricing;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.PricingTests;

/// <summary>
/// Тесты формулы: price = basePrice × ratio^(1 − 0.5) = basePrice × sqrt(ratio)
/// </summary>
public class PricingTests
{
    private readonly ITestOutputHelper _output;
    private readonly IProductRepository _productRepo;

    private static readonly Guid ShopId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public PricingTests(ITestOutputHelper output)
    {
        _output = output;
        _productRepo = Substitute.For<IProductRepository>();
    }

    // ── Вспомогательный метод — создаёт продукт с файлами ───────────────────

    private static void SetProductId(Product product, int id) =>
        typeof(Product).GetProperty("Id")!.SetValue(product, id);

    private static Product MakeProduct(decimal price, params int[] weights)
    {
        var product = new Product("Test", "Desc", ShopId);
        product.AddPrice(price);

        foreach (var w in weights)
        {
            var storage = new StorageObject($"key/{w}", $"file{w}.zip", 1024, "application/zip", ShopId);
            var file = new ContentFile(w, $"file{w}", storage.Id, product.Id);
            product.AddContentFile(file);
        }

        return product;
    }

    // ── CalculatePrice — формула ─────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_AllFiles_ReturnsFullPrice()
    {
        // ratio = 1 → price = basePrice × sqrt(1) = basePrice
        var product = MakeProduct(25m, 10, 8, 7);
        var allFiles = product.ContentFiles.ToList();

        var price = product.CalculatePrice(allFiles, 0.5);

        _output.WriteLine($"All files: {price}");
        Assert.Equal(25.00m, price);
    }

    [Fact]
    public void CalculatePrice_HalfByWeight_AppliesMarkup()
    {
        // weights: 10, 10 → totalWeight = 20
        // selected: первый файл (10 pts) → ratio = 0.5
        // price = 25 × sqrt(0.5) ≈ 25 × 0.7071 ≈ 17.68
        var product = MakeProduct(25m, 10, 10);
        var firstFile = product.ContentFiles.First();

        var price = product.CalculatePrice(new List<ContentFile> { firstFile }, 0.5);

        var expected = Math.Round(25m * (decimal)Math.Sqrt(0.5), 2);
        _output.WriteLine($"50% by weight → price: {price}, expected: {expected}");
        Assert.Equal(expected, price);
        Assert.True(price > 12.5m, "Partial price should be above simple half due to markup");
        Assert.True(price < 25m, "Partial price should be below full price");
    }

    [Fact]
    public void CalculatePrice_20PercentByWeight_ExpensivePerUnit()
    {
        // weights: 10, 10, 10, 10, 10 → totalWeight = 50
        // selected: один файл (10 pts) → ratio = 0.2
        // price = 25 × sqrt(0.2) ≈ 25 × 0.4472 ≈ 11.18
        // простая пропорция была бы: 25 × 0.2 = $5 — наценка очевидна
        var product = MakeProduct(25m, 10, 10, 10, 10, 10);
        var oneFile = product.ContentFiles.First();

        var price = product.CalculatePrice(new List<ContentFile> { oneFile }, 0.5);

        var expected = Math.Round(25m * (decimal)Math.Sqrt(0.2), 2);
        _output.WriteLine($"20% by weight → price: {price}, expected: {expected}, linear would be $5.00");
        Assert.Equal(expected, price);
        // Степенная кривая даёт > 2× линейного при 20%
        Assert.True(price > 10m, "Power curve should charge significantly more than linear at low ratio");
    }

    [Fact]
    public void CalculatePrice_SmallestSlice_StillChargesSignificantMarkup()
    {
        // weights: 9, 1 → totalWeight = 10
        // selected: файл с весом 1 → ratio = 0.1
        // price = 100 × sqrt(0.1) ≈ 100 × 0.3162 ≈ 31.62
        // линейно было бы $10
        var product = MakeProduct(100m, 9, 1);
        var smallFile = product.ContentFiles.Last();

        var price = product.CalculatePrice(new List<ContentFile> { smallFile }, 0.5);

        var expected = Math.Round(100m * (decimal)Math.Sqrt(0.1), 2);
        _output.WriteLine($"10% by weight → price: {price}, linear would be $10.00");
        Assert.Equal(expected, price);
        Assert.True(price > 25m, "At 10% weight, power curve price should be >25% of full price");
    }

    [Theory]
    [InlineData(25,  10, 10, 10, new[] { 0, 1 },    0.67)]  // 20/30 ≈ 0.667 → 25×sqrt(0.667)
    [InlineData(50,  5,  5,  5,  new[] { 0 },        0.33)]  // 5/15 ≈ 0.333 → 50×sqrt(0.333)
    [InlineData(100, 3,  7,  0,  new[] { 1 },        0.70)]  // 7/10 = 0.7   → 100×sqrt(0.7)
    public void CalculatePrice_VariousRatios_MatchFormula(
        decimal basePrice,
        int w0, int w1, int w2,
        int[] selectedIndices,
        double expectedRatioApprox)
    {
        var weights = w2 > 0 ? new[] { w0, w1, w2 } : new[] { w0, w1 };
        var product = MakeProduct(basePrice, weights);
        var files = product.ContentFiles.ToList();
        var selected = selectedIndices.Select(i => files[i]).ToList();

        var totalWeight = weights.Sum();
        var selectedWeight = selectedIndices.Sum(i => weights[i]);
        var ratio = (double)selectedWeight / totalWeight;
        var expected = Math.Round((decimal)((double)basePrice * Math.Pow(ratio, 0.5)), 2);

        var price = product.CalculatePrice(selected, 0.5);

        _output.WriteLine($"ratio≈{ratio:F2}, expected={expected}, got={price}");
        Assert.Equal(expected, price);
    }

    [Fact]
    public void CalculatePrice_EmptyFiles_Throws()
    {
        var product = MakeProduct(25m, 5, 5);

        Assert.Throws<ArgumentException>(() =>
            product.CalculatePrice(new List<ContentFile>(), 0.5));
    }

    [Fact]
    public void CalculatePrice_ProductHasNoFiles_Throws()
    {
        var product = new Product("Empty", "No files", ShopId);
        product.AddPrice(25m);

        Assert.Throws<InvalidOperationException>(() =>
            product.CalculatePrice(new List<ContentFile>(), 0.5));
    }

    // ── CalculateOrderPriceHandler ───────────────────────────────────────────

    [Fact]
    public async Task CalculateOrderPrice_FullPurchase_ReturnsCurrentPrice()
    {
        var product = MakeProduct(25m, 10, 8, 7);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product> { product }));

        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>
            {
                new CheckoutItemDTO { ProductId = product.Id, FileIds = new List<Guid>() }
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Total: {result.Data?.Total}");
        Assert.Equal(Application.DTO.ResponseStatus.Success, result.Status);
        Assert.Equal(25m, result.Data.Total);
        Assert.False(result.Data.Lines[0].IsPartialPurchase);
        Assert.Equal(3, result.Data.Lines[0].SelectedFilesCount);
    }

    [Fact]
    public async Task CalculateOrderPrice_PartialPurchase_AppliesPowerCurve()
    {
        // weights: 10, 10 → selecting one → ratio = 0.5 → price = 25×sqrt(0.5)
        var product = MakeProduct(25m, 10, 10);
        var firstFileId = product.ContentFiles.First().Id;

        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product> { product }));

        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>
            {
                new CheckoutItemDTO { ProductId = product.Id, FileIds = new List<Guid> { firstFileId } }
            }),
            CancellationToken.None);

        // Хендлер использует динамический alpha = PricingConfig.ComputeAlpha(totalFiles)
        // При CapN=30, MinOvershoot=1.2, MaxOvershoot=2.0, totalFiles=2: alpha ≈ 0.553
        var pricingCfg = new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 };
        var alpha      = pricingCfg.ComputeAlpha(product.ContentFiles.Count);
        var expected   = Math.Round(25m * (decimal)Math.Pow(0.5, alpha), 2);
        _output.WriteLine($"Partial: {result.Data?.Total}, expected: {expected} (alpha={alpha:F4})");
        Assert.Equal(Application.DTO.ResponseStatus.Success, result.Status);
        Assert.Equal(expected, result.Data.Total);
        Assert.True(result.Data.Lines[0].IsPartialPurchase);
    }

    [Fact]
    public async Task CalculateOrderPrice_MultipleProducts_SumsCorrectly()
    {
        var p1 = MakeProduct(25m, 10, 10); // full → $25
        var p2 = MakeProduct(20m, 5, 5);   // full → $20
        SetProductId(p1, 1);
        SetProductId(p2, 2);

        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product> { p1, p2 }));

        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>
            {
                new CheckoutItemDTO { ProductId = p1.Id, FileIds = new List<Guid>() },
                new CheckoutItemDTO { ProductId = p2.Id, FileIds = new List<Guid>() }
            }),
            CancellationToken.None);

        _output.WriteLine($"Total: {result.Data?.Total}");
        Assert.Equal(Application.DTO.ResponseStatus.Success, result.Status);
        Assert.Equal(45m, result.Data.Total);
        Assert.Equal(2, result.Data.Lines.Count);
    }

    [Fact]
    public async Task CalculateOrderPrice_EmptyItems_ReturnsError()
    {
        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>()),
            CancellationToken.None);

        Assert.Equal(Application.DTO.ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task CalculateOrderPrice_ProductNotFound_ReturnsError()
    {
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product>())); // пусто — продукт не найден

        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>
            {
                new CheckoutItemDTO { ProductId = 999, FileIds = new List<Guid>() }
            }),
            CancellationToken.None);

        Assert.Equal(Application.DTO.ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task CalculateOrderPrice_InvalidFileId_ReturnsError()
    {
        var product = MakeProduct(25m, 10, 10);
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product> { product }));

        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>
            {
                new CheckoutItemDTO { ProductId = product.Id, FileIds = new List<Guid> { Guid.NewGuid() } }
            }),
            CancellationToken.None);

        // несуществующий fileId → ошибка "not found"
        Assert.Equal(Application.DTO.ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task CalculateOrderPrice_RepositoryThrows_ReturnsError()
    {
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Throws(new Exception("DB timeout"));

        var handler = new CalculateOrderPriceHandler(_productRepo, Substitute.For<IAccountRepository>(), Substitute.For<IContentFileRepository>(), Substitute.For<IContentAccessRepository>(), Options.Create(new PricingConfig { CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0 }));
        var result = await handler.Handle(
            new CalculateOrderPriceQuery(new List<CheckoutItemDTO>
            {
                new CheckoutItemDTO { ProductId = 1, FileIds = new List<Guid>() }
            }),
            CancellationToken.None);

        Assert.Equal(Application.DTO.ResponseStatus.Error, result.Status);
    }
}

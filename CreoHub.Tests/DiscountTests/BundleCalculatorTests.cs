using CreoHub.Domain.Services;
using Xunit;
using Xunit.Abstractions;

namespace CreoHub.Tests.DiscountTests;

/// <summary>
/// Тесты BundleCalculator — частичное ценообразование набора.
///
/// Формула: effectivePrice_i = basePrice_i × (nonOwnedWeight_i / totalWeight_i)^α
///          finalPrice = Σ effectivePrice_i × (1 − bundleDiscFraction)
///          порог 50%: Σ ownedValue_i / Σ basePrice_i > 0.5
/// α вычисляется динамически через переданный делегат computeAlpha(totalFiles).
/// </summary>
public class BundleCalculatorTests
{
    private readonly ITestOutputHelper _output;

    public BundleCalculatorTests(ITestOutputHelper output) => _output = output;

    // ── GetBundleDiscountFraction ────────────────────────────────────────────

    [Theory]
    [InlineData(45, 50, 0.10)]   // 10% скидка: (50-45)/50
    [InlineData(40, 50, 0.20)]   // 20% скидка
    [InlineData(50, 50, 0.00)]   // 0% — цена = сумма детей
    [InlineData(60, 50, 0.00)]   // цена выше суммы детей → 0 (не отрицательная)
    [InlineData(0,  0,  0.00)]   // деление на 0 → 0
    public void GetBundleDiscountFraction_ReturnsCorrectValue(double bundlePrice, double sumChildren, double expected)
    {
        var result = BundleCalculator.GetBundleDiscountFraction((decimal)bundlePrice, (decimal)sumChildren);
        Assert.Equal((decimal)expected, Math.Round(result, 10));
    }

    // ── EffectiveNonOwnedPrice ───────────────────────────────────────────────

    [Theory]
    [InlineData(15, 7, 0,  15.0,   0.5)]   // ничего не куплено → полная цена (ratio=1 → ^any = 1)
    [InlineData(15, 7, 7,   0.0,   0.5)]   // всё куплено → 0
    [InlineData(15, 4, 2,  10.606, 0.5)]   // 2 из 4 куплено: 15*(2/4)^0.5 ≈ 10.606
    [InlineData(15, 0, 0,   0.0,   0.5)]   // нулевой вес → 0
    public void EffectiveNonOwnedPrice_ReturnsCorrectValue(
        double basePrice, int totalWeight, int ownedWeight, double expectedApprox, double alpha)
    {
        var result = BundleCalculator.EffectiveNonOwnedPrice((decimal)basePrice, totalWeight, ownedWeight, alpha);
        _output.WriteLine($"nonOwned price = {result}");
        Assert.Equal((double)result, expectedApprox, 2); // точность 2 знака после запятой
    }

    // ── EffectiveOwnedValue ──────────────────────────────────────────────────

    [Theory]
    [InlineData(15, 7, 0, 0.0,    0.5)]    // ничего не куплено → 0
    [InlineData(15, 4, 4, 15.0,   0.5)]    // всё куплено → полная цена (ratio=1)
    [InlineData(15, 4, 2, 10.606, 0.5)]    // 2 из 4: 15*(2/4)^0.5
    public void EffectiveOwnedValue_ReturnsCorrectValue(
        double basePrice, int totalWeight, int ownedWeight, double expectedApprox, double alpha)
    {
        var result = BundleCalculator.EffectiveOwnedValue((decimal)basePrice, totalWeight, ownedWeight, alpha);
        Assert.Equal((double)result, expectedApprox, 2);
    }

    // ── Calculate — полный расчёт (никто ничего не купил) ───────────────────

    [Fact]
    public void Calculate_NoOwnership_ReturnsBundlePriceWithDiscount()
    {
        // Bundle: 3 × $15 = $45 по-штучно, bundlePrice=$36 → discFrac=0.20
        // Никто ничего не купил → nonOwned/total = 1 → effectivePrice = basePrice
        // adjustedSubtotal = 45, finalPrice = 45*(1-0.2) = 36
        var children = new List<(decimal, int, int, int)>
        {
            (15m, 7, 0, 1),
            (15m, 9, 0, 1),
            (15m, 8, 0, 1),
        };

        var result = BundleCalculator.Calculate(36m, children);

        _output.WriteLine($"FinalPrice={result.FinalPrice}, disc={result.BundleDiscountFraction}");
        Assert.Equal(0.20m, Math.Round(result.BundleDiscountFraction, 10));
        Assert.Equal(36m, Math.Round(result.FinalPrice, 2));
        Assert.False(result.ExceedsThreshold);
        Assert.Equal(0m, result.OwnedFraction);
    }

    // ── Calculate — частичное владение, ниже порога 50% ─────────────────────

    [Fact]
    public void Calculate_PartialOwnership_Below50Pct_RecalculatesPrice()
    {
        // Bundle: 3 дочерних продукта по $15, bundlePrice=$36 → 20% discFrac
        // Пользователь купил: из Item1 (totalWeight=7) куплено 3 (ownedWeight=3)
        var children = new List<(decimal, int, int, int)>
        {
            (15m, 7, 3, 1),  // 3 из 7 куплено
            (15m, 9, 0, 1),
            (15m, 8, 0, 1),
        };

        var result = BundleCalculator.Calculate(36m, children);

        _output.WriteLine($"FinalPrice={result.FinalPrice}, ownedFrac={result.OwnedFraction}");

        Assert.False(result.ExceedsThreshold);
        Assert.True(result.FinalPrice < 36m);    // дешевле полного набора
        Assert.True(result.FinalPrice > 0m);
        Assert.True(result.OwnedFraction < 0.5m);
    }

    // ── Calculate — порог 50% превышен → ExceedsThreshold ───────────────────

    [Fact]
    public void Calculate_Ownership_Above50Pct_ExceedsThreshold()
    {
        // Bundle: 3 × $15 = $45, bundlePrice=$36
        // Куплено: Item1 всё (7/7) + Item2 всё (9/9) → ownedValue = 15+15 = 30 из 45 = 66.7%
        var children = new List<(decimal, int, int, int)>
        {
            (15m, 7, 7, 1),  // полностью куплен
            (15m, 9, 9, 1),  // полностью куплен
            (15m, 8, 0, 1),
        };

        var result = BundleCalculator.Calculate(36m, children);

        _output.WriteLine($"ExceedsThreshold={result.ExceedsThreshold}, ownedFrac={result.OwnedFraction}");

        Assert.True(result.ExceedsThreshold);
        Assert.True(result.OwnedFraction > 0.5m);
    }

    // ── Calculate — все купили → finalPrice = 0 ─────────────────────────────

    [Fact]
    public void Calculate_AllOwned_ReturnsZeroFinalPrice()
    {
        var children = new List<(decimal, int, int, int)>
        {
            (15m, 7, 7, 1),
            (15m, 9, 9, 1),
            (15m, 8, 8, 1),
        };

        var result = BundleCalculator.Calculate(36m, children);

        Assert.True(result.ExceedsThreshold);  // 100% > 50%
        Assert.Equal(0m, result.FinalPrice);    // нечего покупать
    }

    // ── Calculate — пустой список детей → bundlePrice без скидки ────────────

    [Fact]
    public void Calculate_EmptyChildren_ReturnsBundlePriceAsIs()
    {
        var result = BundleCalculator.Calculate(40m, new List<(decimal, int, int, int)>());

        Assert.Equal(40m, result.FinalPrice);
        Assert.False(result.ExceedsThreshold);
        Assert.Equal(0m, result.BundleDiscountFraction);
    }

    // ── Итоговый сценарий: α=0.5, 20% скидка набора, Item1 частично куплен ───

    [Fact]
    public void Calculate_ExcelScenario_Item1PartiallyOwned_20PctBundleDiscount()
    {
        // Item1: $15, totalWeight=7, bought 3 → nonOwnedWeight=4
        //   effectivePrice1 = 15 * (4/7)^0.5 ≈ 11.339
        // Item2: $15, всё свободно → effectivePrice2 = 15
        // Item3: $15, всё свободно → effectivePrice3 = 15
        // bundlePrice = $36 (из $45 со скидкой 20%)
        // adjustedSubtotal ≈ 41.339
        // finalPrice ≈ 41.339 * 0.8 ≈ 33.07

        var children = new List<(decimal, int, int, int)>
        {
            (15m, 7, 3, 1),
            (15m, 9, 0, 1),
            (15m, 8, 0, 1),
        };

        // Фиксируем α=0.5 для воспроизводимости теста
        var result = BundleCalculator.Calculate(36m, children, computeAlpha: _ => 0.5);

        _output.WriteLine($"Adjusted subtotal={result.AdjustedSubtotal:F3}, finalPrice={result.FinalPrice:F3}");
        _output.WriteLine($"Disc={result.BundleDiscountFraction:P2}, ownedFrac={result.OwnedFraction:P2}");

        var nonOwned1        = 15m * (decimal)Math.Sqrt(4.0 / 7.0);
        var expectedSubtotal = nonOwned1 + 15m + 15m;
        var expectedFinal    = expectedSubtotal * 0.8m;

        Assert.Equal(Math.Round(expectedSubtotal, 4), Math.Round(result.AdjustedSubtotal, 4));
        Assert.Equal(Math.Round(expectedFinal,    4), Math.Round(result.FinalPrice,       4));
        Assert.False(result.ExceedsThreshold);
    }
}

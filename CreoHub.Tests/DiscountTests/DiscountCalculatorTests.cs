using CreoHub.Domain.Services;
using Xunit.Abstractions;

namespace CreoHub.Tests.DiscountTests;

/// <summary>
/// Тесты DiscountCalculator: GetCartVolumeDiscount, GetTotalDiscount, ApplyDiscount (Блок F2).
/// </summary>
public class DiscountCalculatorTests
{
    private readonly ITestOutputHelper _output;

    public DiscountCalculatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── GetCartVolumeDiscount — все пороги ───────────────────────────────────

    [Theory]
    [InlineData(0,      0.00)]
    [InlineData(10,     0.00)]
    [InlineData(49.99,  0.00)]
    [InlineData(50,     0.03)]
    [InlineData(75,     0.03)]
    [InlineData(99.99,  0.03)]
    [InlineData(100,    0.06)]
    [InlineData(150,    0.06)]
    [InlineData(199.99, 0.06)]
    [InlineData(200,    0.09)]
    [InlineData(500,    0.09)]
    [InlineData(1000,   0.09)]
    public void GetCartVolumeDiscount_ReturnsCorrectRate(double cartTotalD, double expectedD)
    {
        var cartTotal = (decimal)cartTotalD;
        var expected  = (decimal)expectedD;

        var result = DiscountCalculator.GetCartVolumeDiscount(cartTotal);

        _output.WriteLine($"cartTotal={cartTotal} → discount={result} (expected={expected})");
        Assert.Equal(expected, result);
    }

    // ── GetTotalDiscount ─────────────────────────────────────────────────────

    [Fact]
    public void GetTotalDiscount_SumsDiscounts()
    {
        // 6% lifetime + 3% cart = 9%
        var total = DiscountCalculator.GetTotalDiscount(0.06m, 0.03m);
        Assert.Equal(0.09m, total);
    }

    [Fact]
    public void GetTotalDiscount_MaxCase_CapsAtOne()
    {
        // pathological: 0.99 + 0.99 = 1.98 → capped to 1
        var total = DiscountCalculator.GetTotalDiscount(0.99m, 0.99m);
        Assert.Equal(1m, total);
    }

    [Fact]
    public void GetTotalDiscount_MaxRealCase_12Plus9_Is21Percent()
    {
        // 12% lifetime + 9% cart = 21%
        var total = DiscountCalculator.GetTotalDiscount(0.12m, 0.09m);
        Assert.Equal(0.21m, total);
    }

    [Fact]
    public void GetTotalDiscount_BothZero_ReturnsZero()
    {
        var total = DiscountCalculator.GetTotalDiscount(0m, 0m);
        Assert.Equal(0m, total);
    }

    // ── ApplyDiscount ────────────────────────────────────────────────────────

    [Fact]
    public void ApplyDiscount_ZeroDiscount_ReturnsFull()
    {
        var result = DiscountCalculator.ApplyDiscount(100m, 0m);
        Assert.Equal(100m, result);
    }

    [Fact]
    public void ApplyDiscount_TenPercent_Returns90()
    {
        var result = DiscountCalculator.ApplyDiscount(100m, 0.10m);
        Assert.Equal(90m, result);
    }

    [Fact]
    public void ApplyDiscount_21Percent_CorrectAmount()
    {
        // max real discount: 12% + 9% = 21%
        var result = DiscountCalculator.ApplyDiscount(100m, 0.21m);
        Assert.Equal(79m, result);
    }

    [Fact]
    public void ApplyDiscount_FullDiscount_ReturnsZero()
    {
        var result = DiscountCalculator.ApplyDiscount(100m, 1m);
        Assert.Equal(0m, result);
    }

    // ── Комбинированный сценарий ─────────────────────────────────────────────

    [Fact]
    public void FullFlow_Lifetime6_Cart3_On120Dollars()
    {
        // subtotal = $120, lifetime 6% (spent ≥ $1000), cart 6% ($100–$199)
        const decimal subtotal = 120m;
        var lifetimeDisc   = 0.06m;
        var cartDisc       = DiscountCalculator.GetCartVolumeDiscount(subtotal);  // 0.06
        var totalDisc      = DiscountCalculator.GetTotalDiscount(lifetimeDisc, cartDisc); // 0.12
        var buyerPays      = DiscountCalculator.ApplyDiscount(subtotal, totalDisc); // 105.60

        _output.WriteLine($"subtotal={subtotal}, cartDisc={cartDisc}, totalDisc={totalDisc}, buyerPays={buyerPays}");

        Assert.Equal(0.06m, cartDisc);
        Assert.Equal(0.12m, totalDisc);
        Assert.Equal(105.60m, buyerPays);
    }
}

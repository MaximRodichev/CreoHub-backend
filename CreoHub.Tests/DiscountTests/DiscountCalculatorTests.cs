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

    // ── GetCartCountDiscount — все пороги ────────────────────────────────────

    [Theory]
    [InlineData(0,  0.00)]
    [InlineData(1,  0.00)]
    [InlineData(2,  0.00)]
    [InlineData(3,  0.03)]
    [InlineData(4,  0.03)]
    [InlineData(5,  0.06)]
    [InlineData(7,  0.06)]
    [InlineData(8,  0.09)]
    [InlineData(11, 0.09)]
    [InlineData(12, 0.12)]
    [InlineData(20, 0.12)]
    public void GetCartCountDiscount_ReturnsCorrectRate(int count, double expectedD)
    {
        var result = DiscountCalculator.GetCartCountDiscount(count);
        Assert.Equal((decimal)expectedD, result);
    }

    // ── GetTotalDiscount (MAX logic) ─────────────────────────────────────────

    [Fact]
    public void GetTotalDiscount_TakesMax_LifetimeWins()
    {
        // lifetime 6% > cart 3% → MAX = 6%
        var total = DiscountCalculator.GetTotalDiscount(0.06m, 0.03m);
        Assert.Equal(0.06m, total);
    }

    [Fact]
    public void GetTotalDiscount_TakesMax_CartWins()
    {
        // cart 9% > lifetime 5% → MAX = 9%
        var total = DiscountCalculator.GetTotalDiscount(0.05m, 0.09m);
        Assert.Equal(0.09m, total);
    }

    [Fact]
    public void GetTotalDiscount_TakesMax_BothEqual()
    {
        // both 6% → MAX = 6%
        var total = DiscountCalculator.GetTotalDiscount(0.06m, 0.06m);
        Assert.Equal(0.06m, total);
    }

    [Fact]
    public void GetTotalDiscount_MaxRealCase_9Vs12_Returns12()
    {
        // lifetime 9% (top tier), cart 12% (12 items) → MAX = 12%
        var total = DiscountCalculator.GetTotalDiscount(0.09m, 0.12m);
        Assert.Equal(0.12m, total);
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
    public void ApplyDiscount_12Percent_CorrectAmount()
    {
        // max real discount: MAX(9% lifetime, 12% cart) = 12%
        var result = DiscountCalculator.ApplyDiscount(100m, 0.12m);
        Assert.Equal(88m, result);
    }

    [Fact]
    public void ApplyDiscount_FullDiscount_ReturnsZero()
    {
        var result = DiscountCalculator.ApplyDiscount(100m, 1m);
        Assert.Equal(0m, result);
    }

    // ── Комбинированный сценарий ─────────────────────────────────────────────

    [Fact]
    public void FullFlow_Lifetime5_Count3_TakesMax()
    {
        // 3 items: count discount 3%, lifetime 5% (spent ≥ $1000)
        // MAX(0.05, 0.03) = 0.05, total = $120 × 0.95 = $114
        const decimal subtotal   = 120m;
        const decimal lifetimeDisc = 0.05m;    // $1000+ tier
        var cartDisc             = DiscountCalculator.GetCartCountDiscount(3);  // 0.03
        var totalDisc            = DiscountCalculator.GetTotalDiscount(lifetimeDisc, cartDisc); // MAX = 0.05
        var buyerPays            = DiscountCalculator.ApplyDiscount(subtotal, totalDisc); // 114

        _output.WriteLine($"subtotal={subtotal}, cartDisc={cartDisc}, totalDisc={totalDisc}, buyerPays={buyerPays}");

        Assert.Equal(0.03m, cartDisc);
        Assert.Equal(0.05m, totalDisc);
        Assert.Equal(114m, buyerPays);
    }

    [Fact]
    public void FullFlow_Lifetime0_Count12_TakesCartMax()
    {
        // 12 items, no lifetime → count 12% wins
        // MAX(0, 0.12) = 0.12, total = $300 × 0.88 = $264
        const decimal subtotal    = 300m;
        const decimal lifetimeDisc = 0m;
        var cartDisc              = DiscountCalculator.GetCartCountDiscount(12);  // 0.12
        var totalDisc             = DiscountCalculator.GetTotalDiscount(lifetimeDisc, cartDisc);
        var buyerPays             = DiscountCalculator.ApplyDiscount(subtotal, totalDisc);

        Assert.Equal(0.12m, cartDisc);
        Assert.Equal(0.12m, totalDisc);
        Assert.Equal(264m, buyerPays);
    }

    // ── GetBestDiscountAmount — велком капается на $10 ────────────────────────

    [Fact]
    public void BestAmount_FirstOrder_SmallCart_FullWelcomeUnderCap()
    {
        // $40 первый заказ: 20% = $8 < $10 кап → полные 20% = $8
        var amt = DiscountCalculator.GetBestDiscountAmount(40m, 0.20m, 0m, 0m, 10m);
        Assert.Equal(8m, amt);
    }

    [Fact]
    public void BestAmount_FirstOrder_AtCapBoundary_Returns10()
    {
        // $50 первый заказ: 20% = $10 = кап → ровно $10
        var amt = DiscountCalculator.GetBestDiscountAmount(50m, 0.20m, 0m, 0m, 10m);
        Assert.Equal(10m, amt);
    }

    [Fact]
    public void BestAmount_FirstOrder_LargeSingle_WelcomeCappedAt10()
    {
        // $300 первый заказ, 1 товар (cart 0): 20% = $60, но кап → $10, а не $60
        var amt = DiscountCalculator.GetBestDiscountAmount(300m, 0.20m, 0m, 0m, 10m);
        Assert.Equal(10m, amt);
    }

    [Fact]
    public void BestAmount_FirstOrder_BigBulk_VolumeBeatsCappedWelcome()
    {
        // $300 первый заказ + опт 12% : max(capped welcome $10, объём $36) = $36
        // Это и есть фикс «новичок с большим оптом получает объёмную, а не куцый велком».
        var amt = DiscountCalculator.GetBestDiscountAmount(300m, 0.20m, 0m, 0.12m, 10m);
        Assert.Equal(36m, amt);
    }

    [Fact]
    public void BestAmount_ReturningUser_LoyaltyAndVolume_NoWelcomeNoCap()
    {
        // Не первый заказ (welcome 0): max(лояльность 9% = $27, объём 12% = $36) = $36, без капа
        var amt = DiscountCalculator.GetBestDiscountAmount(300m, 0m, 0.09m, 0.12m, 10m);
        Assert.Equal(36m, amt);
    }

    [Fact]
    public void BestAmount_ZeroSubtotal_ReturnsZero()
    {
        var amt = DiscountCalculator.GetBestDiscountAmount(0m, 0.20m, 0.09m, 0.12m, 10m);
        Assert.Equal(0m, amt);
    }
}

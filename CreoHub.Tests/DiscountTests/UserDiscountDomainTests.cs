using CreoHub.Domain.Entities;
using Xunit.Abstractions;

namespace CreoHub.Tests.DiscountTests;

/// <summary>
/// Тесты доменных методов User: AddSpend и GetLifetimeDiscount (Блок F1).
/// </summary>
public class UserDiscountDomainTests
{
    private readonly ITestOutputHelper _output;

    public UserDiscountDomainTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static User MakeUser() =>
        User.Create("TestUser", "test@test.com");

    // ── AddSpend ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddSpend_PositiveAmount_IncreasesLifetimeSpent()
    {
        var user = MakeUser();
        user.AddSpend(50m);
        user.AddSpend(30m);

        Assert.Equal(80m, user.LifetimeSpent);
    }

    [Fact]
    public void AddSpend_ZeroAmount_ThrowsArgumentException()
    {
        var user = MakeUser();
        Assert.Throws<ArgumentException>(() => user.AddSpend(0m));
    }

    [Fact]
    public void AddSpend_NegativeAmount_ThrowsArgumentException()
    {
        var user = MakeUser();
        Assert.Throws<ArgumentException>(() => user.AddSpend(-10m));
    }

    [Fact]
    public void AddSpend_Cumulative_AccumulatesCorrectly()
    {
        var user = MakeUser();
        for (int i = 0; i < 10; i++)
            user.AddSpend(100m);

        Assert.Equal(1000m, user.LifetimeSpent);
    }

    // ── GetLifetimeDiscount — все пороги ─────────────────────────────────────

    [Theory]
    [InlineData(0,    0.00)]
    [InlineData(100,  0.00)]
    [InlineData(249,  0.00)]
    [InlineData(250,  0.02)]
    [InlineData(499,  0.02)]
    [InlineData(500,  0.03)]
    [InlineData(999,  0.03)]
    [InlineData(1000, 0.05)]
    [InlineData(2499, 0.05)]
    [InlineData(2500, 0.07)]
    [InlineData(4999, 0.07)]
    [InlineData(5000, 0.09)]
    [InlineData(9999, 0.09)]
    public void GetLifetimeDiscount_ReturnsCorrectRate(double spentD, double expectedD)
    {
        var spent            = (decimal)spentD;
        var expectedDiscount = (decimal)expectedD;

        var user = MakeUser();
        if (spent > 0)
            user.AddSpend(spent);

        var discount = user.GetLifetimeDiscount();

        _output.WriteLine($"Spent={spent} → discount={discount} (expected={expectedDiscount})");
        Assert.Equal(expectedDiscount, discount);
    }

    [Fact]
    public void LifetimeSpent_InitialValue_IsZero()
    {
        var user = MakeUser();
        Assert.Equal(0m, user.LifetimeSpent);
    }

    [Fact]
    public void GetLifetimeDiscount_WithZeroSpent_ReturnsZero()
    {
        var user = MakeUser();
        Assert.Equal(0m, user.GetLifetimeDiscount());
    }

    // ── Welcome / первый заказ (лид-магнит −20%) ─────────────────────────────

    [Fact]
    public void IsFirstOrder_NewUser_IsTrue()
    {
        var user = MakeUser();
        Assert.True(user.IsFirstOrder);
    }

    [Fact]
    public void IsFirstOrder_AfterSpend_IsFalse()
    {
        var user = MakeUser();
        user.AddSpend(0.01m);
        Assert.False(user.IsFirstOrder);
    }

    [Fact]
    public void GetWelcomeDiscount_FirstOrder_Returns20Percent()
    {
        var user = MakeUser();
        Assert.Equal(0.20m, user.GetWelcomeDiscount());
    }

    [Fact]
    public void GetWelcomeDiscount_AfterAnyPurchase_ReturnsZero()
    {
        var user = MakeUser();
        user.AddSpend(5m);
        Assert.Equal(0m, user.GetWelcomeDiscount());
    }

    [Fact]
    public void GetPersonalDiscount_FirstOrder_WelcomeBeatsLifetime()
    {
        // На первом заказе lifetime = 0, welcome = 20% → персональная = 20%
        var user = MakeUser();
        Assert.Equal(0.20m, user.GetPersonalDiscount());
    }

    [Theory]
    [InlineData(250,  0.02)]   // welcome уже погас, остаётся lifetime
    [InlineData(5000, 0.09)]
    public void GetPersonalDiscount_AfterSpend_FallsBackToLifetime(double spentD, double expectedD)
    {
        var user = MakeUser();
        user.AddSpend((decimal)spentD);

        var personal = user.GetPersonalDiscount();

        _output.WriteLine($"Spent={spentD} → personal={personal} (expected={expectedD})");
        Assert.Equal((decimal)expectedD, personal);
    }
}

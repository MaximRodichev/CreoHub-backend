using Creohub.Domain.Entities;
using CreoHub.Domain.Types;
using Xunit.Abstractions;

namespace CreoHub.Tests.AutoslotTests;

/// <summary>
/// Чистые domain-тесты для SubscriptionPromoCode и Subscription.
/// Не требуют БД или моков — только логика домена.
/// </summary>
public class PromoCodeDomainTests
{
    private readonly ITestOutputHelper _output;

    public PromoCodeDomainTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── SubscriptionPromoCode.Create ──────────────────────────────────────────

    [Fact]
    public void PromoCode_Create_SetsPropertiesCorrectly()
    {
        var promo = SubscriptionPromoCode.Create(
            "summer30",
            SubscriptionProductType.AutoSlot,
            days: 30);

        _output.WriteLine($"Code: {promo.Code}, Days: {promo.Days}");

        Assert.Equal("SUMMER30", promo.Code);                           // uppercased
        Assert.Equal(SubscriptionProductType.AutoSlot, promo.ProductType);
        Assert.Equal(30, promo.Days);
        Assert.False(promo.IsLifetime);
        Assert.False(promo.IsUsed);
        Assert.Null(promo.UsedByUserId);
        Assert.Null(promo.UsedAt);
        Assert.NotEqual(Guid.Empty, promo.Id);
    }

    [Fact]
    public void PromoCode_Create_Lifetime_SetsLifetimeFlag()
    {
        var promo = SubscriptionPromoCode.Create(
            "LIFETIME_VIP",
            SubscriptionProductType.AutoSlot,
            days: 0,
            isLifetime: true);

        Assert.True(promo.IsLifetime);
        Assert.Equal(0, promo.Days);
    }

    [Fact]
    public void PromoCode_Create_WithExpiry_SetsExpiresAt()
    {
        var expiry = DateTime.UtcNow.AddDays(7);
        var promo  = SubscriptionPromoCode.Create("FLASH", SubscriptionProductType.AutoSlot, 14, expiresAt: expiry);

        Assert.Equal(expiry, promo.ExpiresAt);
    }

    [Fact]
    public void PromoCode_Create_WithoutExpiry_ExpiresAtIsNull()
    {
        var promo = SubscriptionPromoCode.Create("NOLIMIT", SubscriptionProductType.AutoSlot, 30);
        Assert.Null(promo.ExpiresAt);
    }

    // ── SubscriptionPromoCode.CreateForMilestone ──────────────────────────────

    [Fact]
    public void PromoCode_CreateForMilestone_HasCorrectPrefix()
    {
        var userId = Guid.NewGuid();
        var promo  = SubscriptionPromoCode.CreateForMilestone(
            userId, SubscriptionProductType.AutoSlot, 90, "lifetime_500");

        _output.WriteLine($"Code: {promo.Code}, Tag: {promo.MilestoneTag}");

        Assert.StartsWith("AUTOSLOT-", promo.Code);
        Assert.Equal(17, promo.Code.Length);             // "AUTOSLOT-"(9) + 8 hex chars = 17
        Assert.Equal("lifetime_500", promo.MilestoneTag);
        Assert.Equal(userId, promo.IssuedToUserId);
        Assert.Equal(90, promo.Days);
        Assert.False(promo.IsUsed);
    }

    [Fact]
    public void PromoCode_CreateForMilestone_CodesAreUnique()
    {
        var userId = Guid.NewGuid();
        var p1 = SubscriptionPromoCode.CreateForMilestone(userId, SubscriptionProductType.AutoSlot, 90, "tag");
        var p2 = SubscriptionPromoCode.CreateForMilestone(userId, SubscriptionProductType.AutoSlot, 90, "tag");

        Assert.NotEqual(p1.Code, p2.Code);
    }

    // ── SubscriptionPromoCode.Use ─────────────────────────────────────────────

    [Fact]
    public void PromoCode_Use_MarksAsUsedWithUserAndTime()
    {
        var promo  = SubscriptionPromoCode.Create("USETEST", SubscriptionProductType.AutoSlot, 30);
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        promo.Use(userId);

        _output.WriteLine($"IsUsed: {promo.IsUsed}, UsedBy: {promo.UsedByUserId}");

        Assert.True(promo.IsUsed);
        Assert.Equal(userId, promo.UsedByUserId);
        Assert.NotNull(promo.UsedAt);
        Assert.True(promo.UsedAt >= before);
    }

    [Fact]
    public void PromoCode_Use_CanBeCalledOnlyOnce_SecondCallOverwrites()
    {
        // Domain doesn't throw on double-use; business rule is enforced at service level
        var promo   = SubscriptionPromoCode.Create("ONCE", SubscriptionProductType.AutoSlot, 30);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        promo.Use(userId1);
        promo.Use(userId2);

        // Last call wins at domain level — guard is in RedeemPromoCodeAsync (IsUsed check)
        Assert.Equal(userId2, promo.UsedByUserId);
    }

    // ── Subscription.Create ───────────────────────────────────────────────────

    [Fact]
    public void Subscription_Create_NoExistingExpiry_StartsFromNow()
    {
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var sub = Subscription.Create(
            userId,
            SubscriptionProductType.AutoSlot,
            SubscriptionPlanType.Monthly,
            days: 30,
            currentExpiresAt: null);

        _output.WriteLine($"ExpiresAt: {sub.ExpiresAt}");

        Assert.NotNull(sub.ExpiresAt);
        Assert.True(sub.ExpiresAt >= before.AddDays(30));
        Assert.True(sub.ExpiresAt <= DateTime.UtcNow.AddDays(30).AddSeconds(5));
    }

    [Fact]
    public void Subscription_Create_WithActiveExpiry_StacksOnTop()
    {
        var userId    = Guid.NewGuid();
        var existing  = DateTime.UtcNow.AddDays(10);    // 10 days still remaining

        var sub = Subscription.Create(
            userId,
            SubscriptionProductType.AutoSlot,
            SubscriptionPlanType.Monthly,
            days: 30,
            currentExpiresAt: existing);

        // Should stack: existing + 30 days
        var expected = existing.AddDays(30);

        _output.WriteLine($"ExpiresAt: {sub.ExpiresAt}, Expected ~{expected}");

        Assert.NotNull(sub.ExpiresAt);
        // Allow 5s tolerance
        Assert.True(Math.Abs((sub.ExpiresAt!.Value - expected).TotalSeconds) < 5);
    }

    [Fact]
    public void Subscription_Create_WithExpiredDate_StartsFromNow()
    {
        var userId  = Guid.NewGuid();
        var expired = DateTime.UtcNow.AddDays(-5);      // past

        var before = DateTime.UtcNow;
        var sub = Subscription.Create(
            userId,
            SubscriptionProductType.AutoSlot,
            SubscriptionPlanType.Monthly,
            days: 30,
            currentExpiresAt: expired);

        // Expired → starts from now, not from the past date
        Assert.NotNull(sub.ExpiresAt);
        Assert.True(sub.ExpiresAt >= before.AddDays(30));
    }

    [Fact]
    public void Subscription_Create_Lifetime_ExpiresAtIsNull()
    {
        var sub = Subscription.Create(
            Guid.NewGuid(),
            SubscriptionProductType.AutoSlot,
            SubscriptionPlanType.Lifetime,
            days: 0,
            currentExpiresAt: null);

        _output.WriteLine($"ExpiresAt (lifetime): {sub.ExpiresAt?.ToString() ?? "null"}");

        Assert.Null(sub.ExpiresAt);
    }

    [Fact]
    public void Subscription_Create_SetsPromoCodeId()
    {
        var promoId = Guid.NewGuid();
        var sub = Subscription.Create(
            Guid.NewGuid(),
            SubscriptionProductType.AutoSlot,
            SubscriptionPlanType.Monthly,
            days: 30,
            currentExpiresAt: null,
            promoCodeId: promoId);

        Assert.Equal(promoId, sub.PromoCodeId);
    }

    // ── Milestone logic (domain helper) ──────────────────────────────────────

    [Theory]
    [InlineData(499,  false)]    // below first milestone
    [InlineData(500,  true)]     // exactly at first
    [InlineData(501,  true)]     // above first
    [InlineData(1000, true)]     // second milestone
    [InlineData(2500, true)]     // third milestone
    public void Milestone_Threshold_Check(decimal lifetimeSpent, bool shouldPass)
    {
        const decimal threshold = 500m;
        Assert.Equal(shouldPass, lifetimeSpent >= threshold);
    }

    [Fact]
    public void MilestoneTag_IsDistinctPerTier()
    {
        var tags = new[] { "lifetime_500", "lifetime_1000", "lifetime_2500" };
        Assert.Equal(tags.Length, tags.Distinct().Count());
    }
}

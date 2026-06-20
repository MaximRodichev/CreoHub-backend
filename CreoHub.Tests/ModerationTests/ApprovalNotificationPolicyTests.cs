using CreoHub.Application.Commands.AdminCommands;
using Xunit;

namespace CreoHub.Tests.ModerationTests;

/// <summary>
/// Правила выбора уведомления при аппруве товара (#4):
/// первая публикация → NewProduct; повторная — только при изменении цены; иначе молчим.
/// </summary>
public class ApprovalNotificationPolicyTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(0)] // даже без сведений о цене первая публикация = «новый товар»
    public void FirstPublish_AlwaysNewProduct(double current)
    {
        var d = ApprovalNotificationPolicy.Decide(
            everPublished: false, lastPublishedPrice: null, currentPrice: (decimal)current);
        Assert.Equal(ApprovalNotification.NewProduct, d);
    }

    [Fact]
    public void Republish_NoKnownLastPrice_None()
    {
        Assert.Equal(ApprovalNotification.None,
            ApprovalNotificationPolicy.Decide(true, lastPublishedPrice: null, currentPrice: 10m));
    }

    [Fact]
    public void Republish_PriceWentUp_PriceUp()
    {
        Assert.Equal(ApprovalNotification.PriceUp,
            ApprovalNotificationPolicy.Decide(true, lastPublishedPrice: 10m, currentPrice: 12m));
    }

    [Fact]
    public void Republish_PriceWentDown_PriceDown()
    {
        Assert.Equal(ApprovalNotification.PriceDown,
            ApprovalNotificationPolicy.Decide(true, lastPublishedPrice: 10m, currentPrice: 8m));
    }

    [Fact]
    public void Republish_SamePrice_None()
    {
        Assert.Equal(ApprovalNotification.None,
            ApprovalNotificationPolicy.Decide(true, lastPublishedPrice: 10m, currentPrice: 10m));
    }
}

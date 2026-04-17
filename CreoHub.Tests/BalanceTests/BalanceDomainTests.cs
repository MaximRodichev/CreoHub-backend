using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Xunit.Abstractions;

namespace CreoHub.Tests.BalanceTests;

/// <summary>
/// Тесты доменных методов BaseBalance (Spend) и BaseTransaction (SuccessInternal).
/// </summary>
public class BalanceDomainTests
{
    private readonly ITestOutputHelper _output;

    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public BalanceDomainTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── UserBalance.Spend ─────────────────────────────────────────────────────

    [Fact]
    public void Spend_WithSufficientBalance_DeductsAmount()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        balance.Spend(40m);

        _output.WriteLine($"AvailableAmount after Spend(40): {balance.AvailableAmount}");
        Assert.Equal(60m, balance.AvailableAmount);
    }

    [Fact]
    public void Spend_ExactBalance_LeavesZero()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(50m);

        balance.Spend(50m);

        Assert.Equal(0m, balance.AvailableAmount);
    }

    [Fact]
    public void Spend_InsufficientFunds_ThrowsInvalidOperationException()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(30m);

        var ex = Assert.Throws<InvalidOperationException>(() => balance.Spend(50m));
        _output.WriteLine($"Exception: {ex.Message}");
        Assert.Contains("Insufficient", ex.Message);
    }

    [Fact]
    public void Spend_ZeroAmount_ThrowsArgumentException()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        Assert.Throws<ArgumentException>(() => balance.Spend(0m));
    }

    [Fact]
    public void Spend_NegativeAmount_ThrowsArgumentException()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(100m);

        Assert.Throws<ArgumentException>(() => balance.Spend(-10m));
    }

    [Fact]
    public void AddFunds_ThenSpend_PendingAmountUnchanged()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(200m);
        balance.Spend(80m);

        Assert.Equal(0m, balance.PendingAmount);
        Assert.Equal(120m, balance.AvailableAmount);
    }

    // ── BaseTransaction.SuccessInternal ───────────────────────────────────────

    [Fact]
    public void SuccessInternal_SetsStatusToCompleted()
    {
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "balance-test-id");

        transaction.SuccessInternal();

        Assert.Equal(TransactionStatus.Completed, transaction.TransactionStatus);
    }

    [Fact]
    public void SuccessInternal_SetsPaidAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "balance-test-id-2");

        transaction.SuccessInternal();

        Assert.NotNull(transaction.PaidAt);
        Assert.True(transaction.PaidAt > before);
    }

    [Fact]
    public void SuccessInternal_DoesNotSetTxHashOrSenderAddress()
    {
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "balance-test-id-3");

        transaction.SuccessInternal();

        Assert.Null(transaction.TxHash);
        Assert.Null(transaction.SenderAddress);
    }

    [Fact]
    public void SuccessInternal_CalledTwice_ThrowsInvalidOperationException()
    {
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "balance-test-id-4");
        transaction.SuccessInternal();

        Assert.Throws<InvalidOperationException>(() => transaction.SuccessInternal());
    }
}

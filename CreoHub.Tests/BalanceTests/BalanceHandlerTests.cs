using CreoHub.Application.Commands.BalanceCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.PaymentDTOs;
using CreoHub.Application.Queries.Balance;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.BalanceTests;

/// <summary>
/// Тесты хендлеров: GetBalance, TopUpBalance, ConfirmTopUp, CancelPayment (UpBalance path).
/// </summary>
public class BalanceHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IUserBalanceRepository _balanceRepo;
    private readonly IUserTransactionRepository _transactionRepo;
    private readonly IPaymentGatewayService _paymentService;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public BalanceHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _balanceRepo     = Substitute.For<IUserBalanceRepository>();
        _transactionRepo = Substitute.For<IUserTransactionRepository>();
        _paymentService  = Substitute.For<IPaymentGatewayService>();
        _unitOfWork      = Substitute.For<IUnitOfWork>();
    }

    // ── GetBalanceHandler ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetBalance_ExistingBalance_ReturnsCorrectAmounts()
    {
        var balance = new UserBalance(UserId);
        balance.AddFunds(150m);
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);

        var handler = new GetBalanceHandler(_balanceRepo);
        var result  = await handler.Handle(new GetBalanceQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Available: {result.Data?.AvailableAmount}, Pending: {result.Data?.PendingAmount}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(150m, result.Data!.AvailableAmount);
        Assert.Equal(0m,   result.Data.PendingAmount);
    }

    [Fact]
    public async Task GetBalance_NoBalance_ReturnsZeroes()
    {
        _balanceRepo.GetByUserIdAsync(UserId).ReturnsNull();

        var handler = new GetBalanceHandler(_balanceRepo);
        var result  = await handler.Handle(new GetBalanceQuery(UserId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(0m, result.Data!.AvailableAmount);
        Assert.Equal(0m, result.Data.PendingAmount);
    }

    [Fact]
    public async Task GetBalance_WhenRepositoryThrows_ReturnsError()
    {
        _balanceRepo.GetByUserIdAsync(UserId).ThrowsAsync(new Exception("DB error"));

        var handler = new GetBalanceHandler(_balanceRepo);
        var result  = await handler.Handle(new GetBalanceQuery(UserId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB error", result.ErrorMessage);
    }

    // ── TopUpBalanceHandler ───────────────────────────────────────────────────

    [Fact]
    public async Task TopUpBalance_ValidAmount_CreatesInvoiceAndTransaction()
    {
        var invoice = new CreateInvoiceResult(
            TrackId:    "TRK-001",
            PaymentUrl: "https://oxapay.com/pay/abc123",
            ExpiredAt:  DateTime.UtcNow.AddHours(1));
        _paymentService.CreateInvoiceAsync(50m, Arg.Any<string>()).Returns(invoice);

        var handler = new TopUpBalanceHandler(_unitOfWork, _transactionRepo, _paymentService);
        var result  = await handler.Handle(new TopUpBalanceCommand(UserId, 50m), CancellationToken.None);

        _output.WriteLine($"PaymentUrl: {result.Data?.PaymentUrl}, TrackId: {result.Data?.TrackId}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("https://oxapay.com/pay/abc123", result.Data!.PaymentUrl);
        Assert.Equal("TRK-001", result.Data.TrackId);
        await _transactionRepo.Received(1).AddAsync(Arg.Any<UserTransaction>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TopUpBalance_ZeroAmount_ReturnsError()
    {
        var handler = new TopUpBalanceHandler(_unitOfWork, _transactionRepo, _paymentService);
        var result  = await handler.Handle(new TopUpBalanceCommand(UserId, 0m), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await _paymentService.DidNotReceive().CreateInvoiceAsync(Arg.Any<decimal>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TopUpBalance_NegativeAmount_ReturnsError()
    {
        var handler = new TopUpBalanceHandler(_unitOfWork, _transactionRepo, _paymentService);
        var result  = await handler.Handle(new TopUpBalanceCommand(UserId, -10m), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── ConfirmTopUpHandler ───────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmTopUp_NewUser_CreatesBalanceAndAddsFunds()
    {
        var transaction = UserTransaction.CreateUpBalance(75m, UserId, "TRK-100");
        _transactionRepo.GetByTrackIdAsync("TRK-100").Returns(transaction);
        _balanceRepo.GetByUserIdAsync(UserId).ReturnsNull();

        var handler = new ConfirmTopUpHandler(_unitOfWork, _transactionRepo, _balanceRepo);
        var result  = await handler.Handle(
            new ConfirmTopUpCommand("TRK-100", "0xABC", "0xSender", ReceivedAmount: 75m), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _balanceRepo.Received(1).AddAsync(Arg.Is<UserBalance>(b => b.AvailableAmount == 75m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmTopUp_ExistingBalance_AddsToExisting()
    {
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "TRK-200");
        var balance     = new UserBalance(UserId);
        balance.AddFunds(100m);
        _transactionRepo.GetByTrackIdAsync("TRK-200").Returns(transaction);
        _balanceRepo.GetByUserIdAsync(UserId).Returns(balance);

        var handler = new ConfirmTopUpHandler(_unitOfWork, _transactionRepo, _balanceRepo);
        var result  = await handler.Handle(
            new ConfirmTopUpCommand("TRK-200", "0xABC2", "0xSender2", ReceivedAmount: 50m), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(150m, balance.AvailableAmount);
        _balanceRepo.Received(1).Update(Arg.Any<UserBalance>());
    }

    [Fact]
    public async Task ConfirmTopUp_TransactionNotFound_ReturnsError()
    {
        _transactionRepo.GetByTrackIdAsync("NOT-EXIST").ReturnsNull();

        var handler = new ConfirmTopUpHandler(_unitOfWork, _transactionRepo, _balanceRepo);
        var result  = await handler.Handle(
            new ConfirmTopUpCommand("NOT-EXIST", "0xHash", "0xAddr", ReceivedAmount: 0m), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    
        // Ищем русскую подстроку, которая реально возвращается в ErrorMessage
        Assert.Contains("Не удалось зачислить", result.ErrorMessage);
    }
}

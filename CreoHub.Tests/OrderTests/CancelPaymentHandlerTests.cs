using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.OrderTests;

/// <summary>
/// Тесты CancelPaymentHandler — обработка Expired/Failed вебхука от OxaPay.
/// </summary>
public class CancelPaymentHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTransactionRepository _transactionRepo;
    private readonly IOrderRepository _orderRepo;

    private static readonly Guid UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public CancelPaymentHandlerTests(ITestOutputHelper output)
    {
        _output          = output;
        _unitOfWork      = Substitute.For<IUnitOfWork>();
        _transactionRepo = Substitute.For<IUserTransactionRepository>();
        _orderRepo       = Substitute.For<IOrderRepository>();
    }

    private CancelPaymentHandler MakeHandler() =>
        new(_unitOfWork, _transactionRepo, _orderRepo);

    // ── UpBalance: Expired ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelPayment_UpBalance_Expired_SetsExpiredStatus()
    {
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "TRK-EXP-1");
        _transactionRepo.GetByTrackIdAsync("TRK-EXP-1").Returns(transaction);

        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CancelPaymentCommand("TRK-EXP-1", IsExpired: true), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, TxStatus: {transaction.TransactionStatus}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(TransactionStatus.Expired, transaction.TransactionStatus);
        await _orderRepo.DidNotReceive().GetByTransactionIdWithItemsAsync(Arg.Any<Guid>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── UpBalance: Failed ─────────────────────────────────────────────────────

    [Fact]
    public async Task CancelPayment_UpBalance_Failed_SetsFailedStatus()
    {
        var transaction = UserTransaction.CreateUpBalance(50m, UserId, "TRK-FAIL-1");
        _transactionRepo.GetByTrackIdAsync("TRK-FAIL-1").Returns(transaction);

        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CancelPaymentCommand("TRK-FAIL-1", IsExpired: false), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(TransactionStatus.Failed, transaction.TransactionStatus);
    }

    // ── Purchase: Expired — отменяет и заказ ─────────────────────────────────

    [Fact]
    public async Task CancelPayment_Purchase_Expired_CancelsOrderAndTransaction()
    {
        var product = new Product("Slot Pack", "Desc", Guid.NewGuid());
        product.AddPrice(80m);
        var transaction = UserTransaction.CreatePurchase(80m, UserId, "TRK-PURCH-1",
            Order.Open("", new List<(Product, List<ContentFile>)>
            {
                (product, new List<ContentFile>())
            }, UserId));

        var order = Order.Open("", new List<(Product, List<ContentFile>)>
        {
            (product, new List<ContentFile>())
        }, UserId);

        _transactionRepo.GetByTrackIdAsync("TRK-PURCH-1").Returns(transaction);
        _orderRepo.GetByTransactionIdWithItemsAsync(transaction.Id).Returns(order);

        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CancelPaymentCommand("TRK-PURCH-1", IsExpired: true), CancellationToken.None);

        _output.WriteLine($"Order status: {order.Status}, Tx status: {transaction.TransactionStatus}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(TransactionStatus.Expired, transaction.TransactionStatus);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    // ── Транзакция не найдена ─────────────────────────────────────────────────

    [Fact]
    public async Task CancelPayment_TransactionNotFound_ReturnsError()
    {
        _transactionRepo.GetByTrackIdAsync("NO-TRK").ReturnsNull();

        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CancelPaymentCommand("NO-TRK", IsExpired: true), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Не удалось отменить", result.ErrorMessage);
    }

    // ── Purchase без заказа ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelPayment_Purchase_OrderNotFound_ReturnsError()
    {
        var product = new Product("Pack", "Desc", Guid.NewGuid());
        product.AddPrice(30m);
        var transaction = UserTransaction.CreatePurchase(30m, UserId, "TRK-PURCH-2",
            Order.Open("", new List<(Product, List<ContentFile>)>
            {
                (product, new List<ContentFile>())
            }, UserId));

        _transactionRepo.GetByTrackIdAsync("TRK-PURCH-2").Returns(transaction);
        _orderRepo.GetByTransactionIdWithItemsAsync(transaction.Id).ReturnsNull();

        var handler = MakeHandler();
        var result  = await handler.Handle(
            new CancelPaymentCommand("TRK-PURCH-2", IsExpired: false), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Не удалось отменить", result.ErrorMessage);
    }
}

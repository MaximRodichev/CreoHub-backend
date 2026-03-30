using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

/// <summary>
/// Базовая сущность транзакции — движение денег в сервисе
/// </summary>
public abstract class BaseTransaction
{
    protected const decimal WithdrawalFeePercent = 2m;
    protected TransactionStatus _transactionStatus = TransactionStatus.Pending;
    protected decimal _amount;
    protected decimal _platformFeePercent;

    public Guid Id { get; private init; } = Guid.NewGuid();

    public decimal FullAmount
    {
        get => _amount;
        protected init
        {
            if (value <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
            _amount = value;
        }
    }

    public decimal PlatformFeePercent
    {
        get => _platformFeePercent;
        protected init
        {
            if (value < 0 || value > 25)
                throw new ArgumentException("Platform fee percent must be between 0 and 25.");
            _platformFeePercent = value;
        }
    }

    public decimal PlatformFeeAmount { get; protected init; }
    public decimal NetAmount { get; protected init; }

    public TransactionType TransactionType { get; protected init; }

    public TransactionStatus TransactionStatus
    {
        get => _transactionStatus;
        private set
        {
            if (_transactionStatus != TransactionStatus.Pending)
                throw new InvalidOperationException("Only pending transactions can change status.");
            _transactionStatus = value;
        }
    }

    public string? TxHash { get; private set; }
    public string? SenderAddress { get; private set; }
    public string TrackId { get; protected init; }

    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }

    public Order? Order { get; protected init; }

    protected BaseTransaction() {}

    public void Success(string senderAddress, string txHash)
    {
        TransactionStatus = TransactionStatus.Completed;
        SenderAddress = senderAddress ?? throw new ArgumentNullException(nameof(senderAddress));
        TxHash = txHash ?? throw new ArgumentNullException(nameof(txHash));
        PaidAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        TransactionStatus = TransactionStatus.Failed;
    }

    public void Expire()
    {
        TransactionStatus = TransactionStatus.Expired;
    }
}

using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

/// <summary>
/// Базовая сущность транзакции — движение денег в сервисе
/// </summary>
public abstract class BaseTransaction
{
    protected TransactionStatus _transactionStatus = TransactionStatus.Pending;
    protected decimal _amount;

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

    public TransactionType TransactionType { get; protected init; }

    public TransactionStatus TransactionStatus => _transactionStatus;

    public string? TxHash { get; private set; }
    public string? SenderAddress { get; private set; }
    public string TrackId { get; protected init; }

    /// <summary>Фактически полученная сумма (value из вебхука OxaPay, после сетевой комиссии).
    /// Может отличаться от FullAmount (запрошенное). null — для внутренних/незавершённых.</summary>
    public decimal? PaidAmount { get; private set; }

    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }

    public Order? Order { get; protected init; }
    public Guid? OrderId { get; protected init; }

    protected BaseTransaction() {}

    /// <summary>
    /// Оплата получена. Деньги имеют приоритет над статусом: разрешён переход
    /// в Completed даже из Expired/Failed (юзер доплатил после истечения инвойса).
    /// Idempotent: повторный вызов на уже Completed — no-op (защита от дублей вебхука).
    /// </summary>
    public void Success(string senderAddress, string txHash, decimal? paidAmount = null)
    {
        if (_transactionStatus == TransactionStatus.Completed) return;
        _transactionStatus = TransactionStatus.Completed;
        SenderAddress = senderAddress ?? throw new ArgumentNullException(nameof(senderAddress));
        TxHash = txHash ?? throw new ArgumentNullException(nameof(txHash));
        PaidAmount = paidAmount;
        PaidAt = DateTime.UtcNow;
    }

    /// <summary>Помечает неудачу. НЕ перетирает Completed (если оплата уже пришла — гонка вебхуков).</summary>
    public void Fail()
    {
        if (_transactionStatus == TransactionStatus.Completed) return;
        _transactionStatus = TransactionStatus.Failed;
    }

    /// <summary>Помечает истечение. НЕ перетирает Completed (Paid мог прийти раньше Expired).</summary>
    public void Expire()
    {
        if (_transactionStatus == TransactionStatus.Completed) return;
        _transactionStatus = TransactionStatus.Expired;
    }

    /// <summary>
    /// Завершение внутренней транзакции (оплата с баланса) — без txHash и senderAddress.
    /// </summary>
    public void SuccessInternal()
    {
        if (_transactionStatus == TransactionStatus.Completed) return;
        _transactionStatus = TransactionStatus.Completed;
        PaidAt = DateTime.UtcNow;
    }
}

using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class UserTransaction : BaseTransaction
{
    public Guid UserId { get; private init; }

    private UserTransaction() {}

    public static UserTransaction CreateUpBalance(
        decimal amount,
        Guid userId,
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));

        return new UserTransaction
        {
            UserId = userId,
            FullAmount = amount,
            TransactionType = TransactionType.UpBalance,
            TrackId = trackId,
        };
    }

    public static UserTransaction CreatePurchase(
        decimal amount,
        Guid userId,
        string trackId,
        Order order)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));
        if (order == null) throw new ArgumentNullException(nameof(order));

        return new UserTransaction
        {
            UserId = userId,
            FullAmount = amount,
            TransactionType = TransactionType.Purchase,
            TrackId = trackId,
            Order = order,
        };
    }

    /// <summary>
    /// Внутренняя покупка подписки — без привязки к Order (оплата с баланса).
    /// </summary>
    public static UserTransaction CreateSubscriptionPurchase(
        decimal amount,
        Guid userId,
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));

        return new UserTransaction
        {
            UserId = userId,
            FullAmount = amount,
            TransactionType = TransactionType.Purchase,
            TrackId = trackId,
        };
    }

    public static UserTransaction CreateWithdrawal(
        decimal amount,
        Guid userId,
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));

        return new UserTransaction
        {
            UserId = userId,
            FullAmount = amount,
            TransactionType = TransactionType.Withdrawal,
            TrackId = trackId,
        };
    }

    /// <summary>
    /// Перевод с баланса магазина на личный баланс пользователя.
    /// Завершается мгновенно (SuccessInternal).
    /// </summary>
    public static UserTransaction CreateTransfer(
        decimal amount,
        Guid userId,
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));

        var tx = new UserTransaction
        {
            UserId = userId,
            FullAmount = amount,
            TransactionType = TransactionType.Transfer,
            TrackId = trackId,
        };
        tx.SuccessInternal();
        return tx;
    }
}

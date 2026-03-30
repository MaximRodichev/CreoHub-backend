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
            PlatformFeePercent = 0,
            PlatformFeeAmount = 0,
            NetAmount = amount,
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
            PlatformFeePercent = 0,
            PlatformFeeAmount = 0,
            NetAmount = amount,
            TransactionType = TransactionType.Purchase,
            TrackId = trackId,
            Order = order,
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
            PlatformFeePercent = WithdrawalFeePercent,
            PlatformFeeAmount = amount * (WithdrawalFeePercent / 100m),
            NetAmount = amount - (amount * (WithdrawalFeePercent / 100m)),
            TransactionType = TransactionType.Withdrawal,
            TrackId = trackId,
        };
    }
}

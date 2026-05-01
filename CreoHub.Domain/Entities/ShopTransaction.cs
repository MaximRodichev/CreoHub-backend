using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class ShopTransaction : BaseTransaction
{
    private const decimal WithdrawalFeePercent = 0m;
    private decimal _platformFeePercent;

    public Guid ShopId { get; private init; }

    public decimal PlatformFeePercent
    {
        get => _platformFeePercent;
        private init
        {
            if (value < 0 || value > 25)
                throw new ArgumentException("Platform fee percent must be between 0 and 25.");
            _platformFeePercent = value;
        }
    }

    public decimal PlatformFeeAmount { get; private init; }
    public decimal NetAmount { get; private init; }

    private ShopTransaction() {}

    public static ShopTransaction CreateShopSale(
        decimal amount,
        Guid shopId,
        string trackId,
        Order order,
        decimal shopFeePercent = 20)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));
        if (order == null) throw new ArgumentNullException(nameof(order));

        return new ShopTransaction
        {
            ShopId = shopId,
            FullAmount = amount,
            PlatformFeePercent = shopFeePercent,
            PlatformFeeAmount = amount * (shopFeePercent / 100m),
            NetAmount = amount - (amount * (shopFeePercent / 100m)),
            TransactionType = TransactionType.ShopSale,
            TrackId = trackId,
            Order = order,
        };
    }

    public static ShopTransaction CreateWithdrawal(
        decimal amount,
        Guid shopId,
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));

        return new ShopTransaction
        {
            ShopId = shopId,
            FullAmount = amount,
            PlatformFeePercent = WithdrawalFeePercent,
            PlatformFeeAmount = amount * (WithdrawalFeePercent / 100m),
            NetAmount = amount - (amount * (WithdrawalFeePercent / 100m)),
            TransactionType = TransactionType.Withdrawal,
            TrackId = trackId,
        };
    }
}

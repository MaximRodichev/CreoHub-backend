using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

/// <summary>
/// Сущность транзакция, движение денег в сервисе
/// </summary>
public class Transaction
{
    private const decimal WithdrawalFeePercent = 2m;
    private TransactionStatus _transactionStatus = TransactionStatus.Pending;
    private decimal _amount;
    private decimal _platformFeePercent;
    
    public Guid Id { get; private init; } = Guid.NewGuid();
    public OwnerType OwnerType { get; private init; }
    public Guid OwnerId { get; private init; }

    public decimal FullAmount
    {
        get => _amount;
        private init
        {
            if (value <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
            _amount = value;
        }
    }

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
    
    public TransactionType TransactionType { get; private init; }

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
    public string TrackId { get; private init; }
    
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }
    
    public Order? Order { get; private init; }

    private Transaction() {}
    
    public static Transaction CreateWithdrawal(
        decimal amount, 
        OwnerType ownerType, 
        Guid ownerId, 
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));
        
        return new Transaction
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            FullAmount = amount,
            PlatformFeePercent = WithdrawalFeePercent,
            PlatformFeeAmount = amount * (WithdrawalFeePercent / 100m),
            NetAmount = amount - (amount * (WithdrawalFeePercent / 100m)),
            TransactionType = TransactionType.Withdrawal,
            TrackId = trackId,
        };
    }
    
    public static Transaction CreateUpBalance(
        decimal amount, 
        Guid ownerId, 
        string trackId)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));
        
        return new Transaction
        {
            OwnerType = OwnerType.User,
            OwnerId = ownerId,
            FullAmount = amount,
            PlatformFeePercent = 0,
            PlatformFeeAmount = 0,
            NetAmount = amount,
            TransactionType = TransactionType.UpBalance,
            TrackId = trackId,
        };
    }
    
    public static Transaction CreatePurchase(
        decimal amount,
        Guid ownerId, 
        string trackId,
        Order order)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));
        if (order == null) throw new ArgumentNullException(nameof(order));

        return new Transaction
        {
            OwnerType = OwnerType.User,
            OwnerId = ownerId,
            FullAmount = amount,
            PlatformFeePercent = 0,
            PlatformFeeAmount = 0,
            NetAmount = amount,
            TransactionType = TransactionType.Purchase,
            TrackId = trackId,
            Order = order,
        };
    }
    
    public static Transaction CreateShopSale(
        decimal amount,
        Guid ownerId, 
        string trackId,
        Order order,
        decimal shopFeePercent = 20)
    {
        if (trackId == null) throw new ArgumentNullException(nameof(trackId));
        if (order == null) throw new ArgumentNullException(nameof(order));

        return new Transaction
        {
            OwnerType = OwnerType.Shop,
            OwnerId = ownerId,
            FullAmount = amount,
            PlatformFeePercent = shopFeePercent,
            PlatformFeeAmount = amount * (shopFeePercent / 100m),
            NetAmount = amount - (amount * (shopFeePercent / 100m)),
            TransactionType = TransactionType.ShopSale,
            TrackId = trackId,
            Order = order,
        };
    }
    
    public static (Transaction purchase, Transaction shopSale) CreateSalePair(
        decimal amount,
        Guid userId,
        Guid shopId,
        string trackId,
        Order order,
        decimal shopFeePercent)
    {
        var purchase = CreatePurchase(amount, userId, trackId, order);
        var shopSale = CreateShopSale(amount, shopId, trackId, order, shopFeePercent);
        return (purchase, shopSale);
    }

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

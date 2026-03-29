using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Balance
{
    private decimal _availableAmount;
    private decimal _pendingAmount;
    
    public Guid Id { get; private init; } = Guid.NewGuid();
    public OwnerType OwnerType { get; private init; }
    public Guid OwnerId { get; private init; }

    public decimal AvailableAmount
    {
        get => _availableAmount;
        private set
        {
            if (value < 0)
                throw new ArgumentException("Available amount cannot be negative.");
            _availableAmount = value;
        }
    }

    public decimal PendingAmount
    {
        get => _pendingAmount;
        private set
        {
            if (value < 0)
                throw new ArgumentException("Pending amount cannot be negative.");
            _pendingAmount = value;
        }
    }

    private Balance() {}

    public Balance(Guid ownerId, OwnerType ownerType)
    {
        OwnerId = ownerId;
        OwnerType = ownerType;
        AvailableAmount = 0;
        PendingAmount = 0;
    }

    public void AddFunds(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        AvailableAmount += amount;
    }

    public void WithdrawFunds(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        if (amount > AvailableAmount)
            throw new ArgumentException("Insufficient available funds.", nameof(amount));
        if (PendingAmount > 0)
            throw new InvalidOperationException("Another withdrawal is already in progress.");

        PendingAmount += amount;
        AvailableAmount -= amount;
    }

    public void CompleteWithdraw()
    {
        if (PendingAmount == 0)
            throw new InvalidOperationException("No pending withdrawal to complete.");
        PendingAmount = 0;
    }

    public void CancelWithdraw()
    {
        if (PendingAmount == 0)
            throw new InvalidOperationException("No pending withdrawal to cancel.");
        AvailableAmount += PendingAmount;
        PendingAmount = 0;
    }
}

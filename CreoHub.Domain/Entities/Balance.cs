using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Balance
{
    private decimal _availableAmount;
    private decimal _pendingAmount;
    
    public Guid Id { get; init; } = Guid.NewGuid();
    public OwnerType OwnerType { get; private init; }
    public Guid OwnerId { get; private init; }

    public decimal AvailableAmount
    {
        get
        {
            return _availableAmount;
        }
        private set
        {
            if (value < 0)
            {
                throw new ArgumentException("amount must be greater than or equal to zero");
            }
            _availableAmount = value;
        }
    }

    public decimal PendingAmount
    {
        get
        {
            return _pendingAmount;
        }
        private set
        {
            if (value < 0)
            {
                throw new ArgumentException("amount must be greater than or equal to zero");
            }
            _pendingAmount = value;
        }
    }

    private Balance()
    {
        
    }

    public Balance(Guid ownerId, OwnerType ownerType)
    {
        this.OwnerId = ownerId;
        this.OwnerType = ownerType;
        this.AvailableAmount = 0;
        this.PendingAmount = 0;
    }

    public Balance AddFunds(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("amount must be greater than or equal to zero");
        }
        AvailableAmount += amount;
        return this;
    }

    public Balance WithdrawFunds(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("withdraw amount cannot be negative");
        }
        if (amount > AvailableAmount)
        {
            throw new ArgumentException("withdraw amount cannot be greater than available amount");
        }
        if (PendingAmount > 0)
        {
            throw new ArgumentException("Withdraw process not completed");
        }

        PendingAmount += amount;
        AvailableAmount -= amount;
        return this;
    }

    public Balance CompleteWithdraw()
    {
        if (PendingAmount == 0)
        {
            throw new ArgumentException("Withdrawal will be competed yet");
        }
        PendingAmount = 0;
        return this;
    }

    public Balance CanceledWithdraw()
    {
        if (PendingAmount == 0)
        {
            throw new ArgumentException("Withdrawal will be completed yet");
        }
        AvailableAmount += PendingAmount;
        PendingAmount = 0;
        
        return this;
    }
}
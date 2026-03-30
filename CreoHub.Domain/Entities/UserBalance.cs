namespace CreoHub.Domain.Entities;

public class UserBalance : BaseBalance
{
    public Guid UserId { get; private init; }

    private UserBalance() {}

    public UserBalance(Guid userId) : base()
    {
        UserId = userId;
    }
}

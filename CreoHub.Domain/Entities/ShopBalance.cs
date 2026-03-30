namespace CreoHub.Domain.Entities;

public class ShopBalance : BaseBalance
{
    public Guid ShopId { get; private init; }

    private ShopBalance() {}

    public ShopBalance(Guid shopId) : base()
    {
        ShopId = shopId;
    }
}

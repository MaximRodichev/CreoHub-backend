using CreoHub.Domain.Entities;

namespace CreoHub.Domain.Services;

public static class TransactionService
{
    public static (UserTransaction purchase, ShopTransaction shopSale) CreateSalePair(
        decimal amount,
        Guid userId,
        Guid shopId,
        string trackId,
        Order order,
        decimal shopFeePercent)
    {
        var purchase = UserTransaction.CreatePurchase(amount, userId, trackId, order);
        var shopSale = ShopTransaction.CreateShopSale(amount, shopId, trackId, order, shopFeePercent);
        return (purchase, shopSale);
    }
}

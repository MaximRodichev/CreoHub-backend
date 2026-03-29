namespace CreoHub.Domain.Entities;

public class OrderItem
{
    public int Id { get; private init; }
    public Guid OrderId { get; private init; }
    public Order Order { get; private init; }
    public int ProductId { get; private init; }
    public Product Product { get; private init; }
    public decimal PriceAtPurchase { get; private init; }

    private OrderItem() {}

    public OrderItem(Guid orderId, int productId, decimal priceAtPurchase)
    {
        if (priceAtPurchase <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(priceAtPurchase));

        OrderId = orderId;
        ProductId = productId;
        PriceAtPurchase = priceAtPurchase;
    }
}
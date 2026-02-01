namespace CreoHub.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; }

    public decimal PriceAtPurchase { get; set; }
}
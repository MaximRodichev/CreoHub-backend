using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = new();
    private decimal _price;
    
    public Guid Id { get; private init; } = Guid.NewGuid();
    public decimal Price 
    { 
        get => _price; 
        private init => _price = value > 0 ? value 
            : throw new ArgumentException("Price must be greater than zero."); 
    }
    public string Description { get; private init; } = string.Empty;

    public DateTime OrderDate { get; private init; } = DateTime.UtcNow;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    
    //FK
    public User Customer { get; private init; }
    public Guid CustomerId { get; private init; }
    public Transaction? Transaction { get; private set; }
    public Guid? TransactionId { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() {}

    /// <summary>
    /// Создание заказа
    /// </summary>
    /// <returns></returns>
    public static Order Open(string description, List<Product> products, Guid customerId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        if (products == null || products.Count == 0)
            throw new ArgumentException("Products cannot be empty.", nameof(products));

        var order = new Order
        {
            Description = description,
            CustomerId = customerId,
        };

        foreach (var product in products)
        {
            var price = product.Prices
                            .OrderByDescending(p => p.Date)
                            .FirstOrDefault()
                        ?? throw new InvalidOperationException(
                            $"Product {product.Id} has no prices.");

            order._items.Add(new OrderItem(
                orderId: order.Id,
                productId: product.Id,
                priceAtPurchase: price.Value
            ));
        }

        order._price = order._items.Sum(i => i.PriceAtPurchase);

        return order;
    }

    public Order Complete()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be completed.");
        Status = OrderStatus.Completed;
        return this;
    }

    public Order Cancel()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be cancelled.");
        Status = OrderStatus.Cancelled;
        return this;
    }
}
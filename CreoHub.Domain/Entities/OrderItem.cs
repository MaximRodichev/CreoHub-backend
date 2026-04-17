namespace CreoHub.Domain.Entities;

public class OrderItem
{
    private readonly List<OrderItemFile> _files = new();

    public int Id { get; private init; }
    public Guid OrderId { get; private init; }
    public Order Order { get; private init; }
    public int ProductId { get; private init; }
    public Product Product { get; private init; }
    public decimal PriceAtPurchase { get; private init; }

    /// <summary>
    /// Конкретные файлы, выбранные при частичной покупке.
    /// Пустая коллекция = куплен полный продукт (все файлы).
    /// </summary>
    public IReadOnlyCollection<OrderItemFile> Files => _files.AsReadOnly();

    private OrderItem() {}

    public OrderItem(Guid orderId, int productId, decimal priceAtPurchase,
        IEnumerable<Guid>? selectedFileIds = null)
    {
        if (priceAtPurchase <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(priceAtPurchase));

        OrderId = orderId;
        ProductId = productId;
        PriceAtPurchase = priceAtPurchase;

        if (selectedFileIds != null)
            foreach (var fileId in selectedFileIds)
                _files.Add(new OrderItemFile(fileId));
    }
}
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
    public UserTransaction? Transaction { get; private set; }
    public Guid? TransactionId { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() {}


    
    /// <summary>
    /// Создание заказа
    /// </summary>
    public static Order Open(string description,
        List<(Product product, List<ContentFile> selectedFiles)> items,
        Guid customerId)
    {
        // if (string.IsNullOrWhiteSpace(description))
        //     throw new ArgumentException("Description cannot be empty.", nameof(description));
        if (items == null || items.Count == 0)
            throw new ArgumentException("Items cannot be empty.", nameof(items));

        var order = new Order
        {
            Description = description,
            CustomerId = customerId,
        };

        foreach (var (product, selectedFiles) in items)
        {
            IEnumerable<Guid>? purchasedFileIds;
            decimal price;

            if (selectedFiles == null || selectedFiles.Count == 0)
            {
                // Полная покупка — берём текущую цену, файлы не фиксируем
                price = product.GetCurrentPrice();
                purchasedFileIds = null;
            }
            else
            {
                // Частичная покупка — цена по весу выбранных файлов
                price = product.CalculatePrice(selectedFiles);
                purchasedFileIds = selectedFiles.Select(f => f.Id);
            }

            order._items.Add(new OrderItem(
                orderId: order.Id,
                productId: product.Id,
                priceAtPurchase: price,
                selectedFileIds: purchasedFileIds
            ));
        }

        order._price = order._items.Sum(i => i.PriceAtPurchase);

        return order;
    }

    public void AttachTransaction(UserTransaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
        if (Transaction != null)
            throw new InvalidOperationException("Order already has a transaction.");

        Transaction = transaction;
        TransactionId = transaction.Id;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be completed.");
        if (Transaction == null)
            throw new InvalidOperationException("Cannot complete order without a transaction.");

        Status = OrderStatus.Completed;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be cancelled.");
        Status = OrderStatus.Cancelled;
    }
}
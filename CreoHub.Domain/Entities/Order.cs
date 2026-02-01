using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Order
{
    public Guid Id  { get; private set; } = Guid.NewGuid();
    public decimal Price { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime OrderDate { get; private set; } =  DateTime.Now;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    
    //FK
    public User Customer { get; private set; }
    public Guid CustomerId { get; private set; }
    
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    public Order()
    {
        
    }

    /// <summary>
    /// Создание заказа
    /// </summary>
    /// <returns></returns>
    public static Order Open(decimal price, string description, List<Product> products, Guid customerId)
    {
        List<OrderItem> items = new List<OrderItem>(products.Count);
        
        Order thisOrder = new Order()
        {
            Id = Guid.NewGuid(),
            Price = price,
            Description = description,
            CustomerId = customerId,
        };

        foreach (var product in products)
        {
            items.Add(new OrderItem()
            {
                OrderId = thisOrder.Id,
                ProductId = product.Id,
                PriceAtPurchase = product.Prices.Last().Value,
            });
        }
        
        thisOrder.Items = items;
        
        return thisOrder;
    }

    public Order InjectOrderDate(DateTime orderDate)
    {
        OrderDate =  orderDate;
        return this;
    }
}
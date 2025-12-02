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
    public Product Product { get; private set; }
    public int ProductId { get; private set; }

    public Order()
    {
        
    }

    /// <summary>
    /// Создание заказа
    /// </summary>
    /// <returns></returns>
    public static Order Open(decimal price, string description, int productId, Guid customerId)
    {
        return new Order()
        {
            Id = Guid.NewGuid(),
            Price = price,
            Description = description,
            ProductId = productId,
            CustomerId = customerId,
        };
    }

    public Order InjectOrderDate(DateTime orderDate)
    {
        OrderDate =  orderDate;
        return this;
    }
}
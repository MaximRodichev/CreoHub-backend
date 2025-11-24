using CreoHub.CRM.Domain.Types;

namespace CreoHub.CRM.Domain.Entities;

public class Order
{
    public Guid Id  { get; set; }
    public double Price { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    
    //FK
    public User Customer { get; set; }
    public Product Product { get; set; }
}
namespace CreoHub.CRM.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public double Discount { get; set; }
    public double TelegramId { get; set; }
    
    // FK
    public ICollection<Order> Orders { get; set; }
    public Shop Shop { get; set; }
    
    public User()
    {
        
    }
}
namespace CreoHub.CRM.Domain.Entities;

public class Shop
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    
    //FK
    public ICollection<Product> Products { get; set; }
    public User Owner { get; set; }
}
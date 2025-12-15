namespace CreoHub.Domain.Entities;

public class Shop
{
    public Guid Id { get; private set; } =  Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    
    //FK
    public ICollection<Product> Products { get; set; }
    public User Owner { get; set; }
    public Guid OwnerId { get; set; }

    public Shop()
    {
        
    }

    public Shop(string name, string description, Guid ownerId)
    {
        Name = name;
        Description = description;
        OwnerId = ownerId;
    }
}
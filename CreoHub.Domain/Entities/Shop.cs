namespace CreoHub.Domain.Entities;

public class Shop
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    //FK
    public ICollection<Product> Products { get; set; }
    public User Owner { get; set; }

    public Shop()
    {
        
    }

    public Shop(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
namespace CreoHub.CRM.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    
    //FK
    public Shop Owner { get; set; }
    public ICollection<Tag> Tags { get; set; }

    public Product()
    {
        
    }
}
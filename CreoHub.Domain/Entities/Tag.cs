namespace CreoHub.CRM.Domain.Entities;

public class Tag
{
    public int  Id { get; set; }
    public string Name { get; set; }
    
    //FK
    public ICollection<Product> Products { get; set; }
}
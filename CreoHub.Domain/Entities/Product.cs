using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Product
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    public ProductType ProductType { get; set; } = ProductType.Single;
    public ICollection<ProductBundle> BundleItems { get; private set; }
    
    //FK Ef-core
    public Shop Owner { get; set; }
    public Guid OwnerId { get; set; }
    public ICollection<Tag> Tags { get; set; }
    public List<Price> Prices { get; set; }
    public List<Order> Orders { get; set; }

    public Product()
    {
        
    }

    public Product(string name, string description, Shop owner, ICollection<Tag> tags)
    {
        Name = name;
        Description = description;
        Owner = owner;
        Tags = tags;
    }

    public Product InjectDate(DateTime date)
    {
        CreatedAt = date;
        return this;
    }
}
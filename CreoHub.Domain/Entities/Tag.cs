using System.ComponentModel.DataAnnotations.Schema;

namespace CreoHub.Domain.Entities;

public class Tag
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    
    //FK
    public ICollection<Product> Products { get; set; }

    public Tag()
    {
        
    }

    public Tag(string name)
    {
        Name = name;
    }
}
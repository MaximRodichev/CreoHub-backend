using System.ComponentModel.DataAnnotations.Schema;

namespace CreoHub.Domain.Entities;

public class Tag
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; private init; }
    public string Name { get; private init; }
    
    public IReadOnlyCollection<Product> Products { get; private set; }

    private Tag() {}

    public Tag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));
        Name = name;
    }
}
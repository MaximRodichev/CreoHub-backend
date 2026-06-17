using CreoHub.Domain.Entities;

namespace CreoHub.Application.DTO.ProductDTOs;

public class CreateProductDTO
{
    public string Name { get; set; }
    /// <summary>URL товара. null/пусто — сгенерировать из Name (транслит).</summary>
    public string? Slug { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
    public decimal Price { get; set; }
}
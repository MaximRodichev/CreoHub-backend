using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.ProductDTOs;

public class ProductShortInfoDTO
{
    public string Name { get; set; }
    public string? Slug { get; set; }
    public decimal Price { get; set; }
    public int Id { get; set; }
    public ProductType ProductType { get; set; }
}
namespace CreoHub.Application.DTO.ProductDTOs;

public class CreateProductBundleDTO
{
    public ICollection<int> ProductIds { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
}
namespace CreoHub.Application.DTO.ProductDTOs;

public class UpdateProductInfoDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public List<string> Tags { get; set; }
    public List<Guid> ObjectStorageIds { get; set; }
}
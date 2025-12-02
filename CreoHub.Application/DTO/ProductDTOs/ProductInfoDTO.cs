namespace CreoHub.Application.DTO.ProductDTOs;

public record ProductInfoDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int TotalSells { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; }
}
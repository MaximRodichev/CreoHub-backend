namespace CreoHub.Application.DTO.ProductDTOs;

public record ProductViewDTO
{
    public int Id { get; set; }
    public int TotalSells { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public Guid OwnerId  { get; set; }
    public string OwnerName { get; set; }
    public List<string> Tags { get; set; }
    public DateTime Date { get; set; }
}
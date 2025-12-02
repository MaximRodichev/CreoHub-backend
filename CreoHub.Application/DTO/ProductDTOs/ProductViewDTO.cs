namespace CreoHub.Application.DTO.ProductDTOs;

public record ProductViewDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
using CreoHub.Domain.Entities;

namespace CreoHub.Application.DTO.ProductDTOs;

public class CreateProductDTO
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }
    public decimal Price { get; set; }
    
    public DateTime Date { get; set; } // TODO: убрать как только заполню бд
}
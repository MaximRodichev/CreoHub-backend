using CreoHub.Application.DTO.ProductDTOs;

namespace CreoHub.Application.DTO;

public class PageViewDTO
{
    public IReadOnlyList<ProductViewDTO> Products { get; set; }
    public int CountProducts { get; set; }
}
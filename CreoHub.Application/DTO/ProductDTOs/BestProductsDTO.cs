namespace CreoHub.Application.DTO.ProductDTOs;

public class BestProductsDTO
{
    /// <summary>Новинки (сортировка по дате, 6 штук).</summary>
    public List<ProductViewDTO> Newest  { get; set; } = new();
    /// <summary>Популярные (по продажам, 6 штук).</summary>
    public List<ProductViewDTO> Popular { get; set; } = new();
}

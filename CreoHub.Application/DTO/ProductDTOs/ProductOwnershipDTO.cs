namespace CreoHub.Application.DTO.ProductDTOs;

/// <summary>
/// Статус владения продуктами для конкретного пользователя.
/// FullyOwned  — пользователь купил все файлы продукта (или все файлы дочерних продуктов бандла).
/// PartiallyOwned — пользователь купил часть файлов.
/// </summary>
public class ProductOwnershipDTO
{
    public List<int> FullyOwned    { get; set; } = new();
    public List<int> PartiallyOwned { get; set; } = new();
}

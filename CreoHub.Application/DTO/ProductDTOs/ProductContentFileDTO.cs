namespace CreoHub.Application.DTO.ProductDTOs;

/// <summary>
/// Файл продукта с весом. Фронтенд использует priceWeight для пропорционального отображения.
/// Точная цена считается сервером при checkout через /api/payment/calculate.
/// </summary>
public class ProductContentFileDTO
{
    public Guid FileId { get; set; }
    /// <summary>Отображаемое имя файла (PreviewName).</summary>
    public string FileName { get; set; }
    /// <summary>Вес файла (1–10) — определяет долю в итоговой цене.</summary>
    public int PriceWeight { get; set; }
}

namespace CreoHub.Application.DTO.OrderDTOs;

public class PriceBreakdownDTO
{
    public decimal Total { get; set; }
    public List<PriceLineDTO> Lines { get; set; } = new();
}

public class PriceLineDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SelectedFilesCount { get; set; }
    public int TotalFilesCount { get; set; }
    public bool IsPartialPurchase { get; set; }
    public decimal Price { get; set; }
}

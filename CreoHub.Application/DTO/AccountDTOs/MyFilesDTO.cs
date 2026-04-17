namespace CreoHub.Application.DTO.AccountDTOs;

/// <summary>
/// Группа купленных файлов одного продукта.
/// </summary>
public class MyFilesProductGroupDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string? PreviewKey { get; set; }
    public List<MyPurchasedFileDTO> Files { get; set; } = new();
}

public class MyPurchasedFileDTO
{
    public Guid ContentFileId { get; set; }
    public string FileName { get; set; }
    public Guid OrderId { get; set; }
    public DateTime GrantedAt { get; set; }
}

namespace CreoHub.Application.DTO.StorageDTOs;

public class UpdateContentFileDTO
{
    /// <summary>Новое отображаемое имя файла. null = не менять.</summary>
    public string? PreviewName { get; set; }
    /// <summary>Новый вес файла (1-10). null = не менять.</summary>
    public int? PriceWeight { get; set; }
}

public class UpdateMediaSortOrderDTO
{
    public int ProductId { get; set; }
    public Guid StorageObjectId { get; set; }
    public int SortOrder { get; set; }
}

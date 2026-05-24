namespace CreoHub.Application.DTO.StorageDTOs;

public class ContentFileInfo
{
    public Guid Id { get; set; }
    public Guid StorageObjectId { get; set; }
    public string PreviewName { get; set; }
    public int PriceWeight { get; set; }
    /// <summary>Оригинальное имя файла в хранилище (StorageObject.FileName).</summary>
    public string? StorageFileName { get; set; }
    /// <summary>True — файл архивирован (был куплен, скрыт от новых покупателей).</summary>
    public bool IsArchived { get; set; }
}
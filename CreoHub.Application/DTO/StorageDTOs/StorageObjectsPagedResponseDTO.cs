namespace CreoHub.Application.DTO.StorageDTOs;

/// <summary>
/// Пагинированный ответ для GET /s3/files с агрегатами для метрик и счётчиков вкладок.
/// </summary>
public class StorageObjectsPagedResponseDTO
{
    public List<StorageObjectResponseDTO> Items     { get; set; } = new();
    public int  Total      { get; set; }  // кол-во записей, удовлетворяющих текущему фильтру
    public int  Page       { get; set; }
    public int  PageSize   { get; set; }
    public int  TotalPages { get; set; }

    // ── Агрегаты по всем файлам шопа (не зависят от фильтра) ─────────────────
    public long TotalSizeBytes { get; set; }
    public int  CountAll       { get; set; }
    public int  CountContent   { get; set; }
    public int  CountMedia     { get; set; }
    public int  CountUploaded  { get; set; }  // FileType = Unregistred
    public int  CountLocked    { get; set; }  // IsSystemLocked = true
}

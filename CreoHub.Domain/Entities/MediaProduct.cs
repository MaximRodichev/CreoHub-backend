namespace CreoHub.Domain.Entities;

public class MediaProduct
{
    // EF
    public Product Product { get; set; }
    public int ProductId { get; set; }
    public StorageObject StorageObject { get; set; }
    public Guid StorageObjectId { get; set; }
    public StorageObject? Thumbnail { get; set; }
    public Guid? ThumbnailId { get; set; }
    
    public int SortOrder { get; set; }
}
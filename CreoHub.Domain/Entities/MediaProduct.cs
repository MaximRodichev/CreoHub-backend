namespace CreoHub.Domain.Entities;

public class MediaProduct
{
    public Product Product { get; private init; }
    public int ProductId { get; private init; }
    public StorageObject StorageObject { get; private init; }
    public Guid StorageObjectId { get; private init; }
    public StorageObject? Thumbnail { get; private set; }
    public Guid? ThumbnailId { get; private set; }
    
    public int SortOrder { get; private set; }

    private MediaProduct() {}

    public MediaProduct(int productId, Guid storageObjectId, int sortOrder,
        Guid? thumbnailId = null)
    {
        if (sortOrder < 0)
            throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));

        ProductId = productId;
        StorageObjectId = storageObjectId;
        SortOrder = sortOrder;
        ThumbnailId = thumbnailId;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));
        SortOrder = sortOrder;
    }

    public void SetThumbnail(Guid thumbnailId)
    {
        ThumbnailId = thumbnailId;
    }

    public void RemoveThumbnail()
    {
        Thumbnail = null;
        ThumbnailId = null;
    }
}

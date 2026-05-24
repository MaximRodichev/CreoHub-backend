namespace CreoHub.Domain.Entities;

public class ContentFile
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public int PriceWeight { get; private set; }
    public string PreviewName { get; private set; }

    /// <summary>
    /// Дата архивирования. Не null — файл был куплен и не может быть удалён,
    /// но скрыт из активного состава товара для новых покупателей.
    /// </summary>
    public DateTime? ArchivedAt { get; private set; }
    public bool IsArchived => ArchivedAt.HasValue;

    public StorageObject StorageObject { get; private init; }
    public Guid StorageObjectId { get; private init; }
    public Product Product { get; private init; }
    public int ProductId { get; private init; }

    private ContentFile() {}

    public ContentFile(int priceWeight, string previewName, 
        Guid storageObjectId, int productId)
    {
        if (priceWeight < 1 || priceWeight > 10)
            throw new ArgumentOutOfRangeException(nameof(priceWeight), "Must be between 1 and 10.");

        PriceWeight = priceWeight;
        PreviewName = previewName ?? throw new ArgumentNullException(nameof(previewName));
        StorageObjectId = storageObjectId;
        ProductId = productId;
    }

    public void UpdatePriceWeight(int priceWeight)
    {
        if (priceWeight < 1 || priceWeight > 10)
            throw new ArgumentOutOfRangeException(nameof(priceWeight), "Must be between 1 and 10.");
        PriceWeight = priceWeight;
    }

    public void UpdatePreviewName(string previewName)
    {
        PreviewName = previewName ?? throw new ArgumentNullException(nameof(previewName));
    }

    /// <summary>
    /// Архивирует файл: скрывает из активного состава товара,
    /// но сохраняет доступ для существующих покупателей.
    /// </summary>
    public void Archive()
    {
        if (IsArchived) return;
        ArchivedAt = DateTime.UtcNow;
    }
}

namespace CreoHub.Domain.Entities;

public class ProductEditHistory
{
    public int      Id          { get; private set; }
    public int      ProductId   { get; private set; }
    public Guid?    EditorId    { get; private set; }   // shopId который сохранил
    public DateTime CreatedAt   { get; private set; }
    /// <summary>JSON-снимок состояния товара ДО применения изменений.</summary>
    public string   SnapshotJson { get; private set; } = string.Empty;

    public Product Product { get; private set; } = null!;

    private ProductEditHistory() { }

    public ProductEditHistory(int productId, Guid? editorId, string snapshotJson)
    {
        ProductId    = productId;
        EditorId     = editorId;
        CreatedAt    = DateTime.UtcNow;
        SnapshotJson = snapshotJson;
    }
}

namespace CreoHub.Domain.Entities;

public class OrderItemFile
{
    public Guid ContentFileId { get; private init; }

    /// <summary>Navigation property — populated by EF Core when explicitly included.</summary>
    public ContentFile ContentFile { get; private set; } = null!;

    private OrderItemFile() {}

    internal OrderItemFile(Guid contentFileId)
    {
        ContentFileId = contentFileId;
    }
}

namespace CreoHub.Domain.Entities;

public class OrderItemFile
{
    public Guid ContentFileId { get; private init; }

    private OrderItemFile() {}

    internal OrderItemFile(Guid contentFileId)
    {
        ContentFileId = contentFileId;
    }
}

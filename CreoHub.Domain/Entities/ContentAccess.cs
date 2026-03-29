namespace CreoHub.Domain.Entities;

public class ContentAccess
{
    public Guid Id { get; private init; } = Guid.NewGuid();

    public User User { get; private init; }
    public Guid UserId { get; private init; }

    public ContentFile ContentFile { get; private init; }
    public Guid ContentFileId { get; private init; }

    public Order Order { get; private init; }
    public Guid OrderId { get; private init; }

    public DateTime GrantedAt { get; private init; } = DateTime.UtcNow;

    private ContentAccess() {}

    public ContentAccess(Guid userId, Guid contentFileId, Guid orderId)
    {
        UserId = userId;
        ContentFileId = contentFileId;
        OrderId = orderId;
    }
}

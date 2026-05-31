namespace CreoHub.Domain.Entities;

/// <summary>
/// Lightweight behavioural event for analytics (product_view, cart_add, checkout_started, etc.).
/// Inserted in bulk by EventTrackerBackgroundService — never part of a UnitOfWork transaction.
/// </summary>
public class UserEvent
{
    public long    Id        { get; private init; }
    public string  EventType { get; private init; } = null!;
    public int?    ProductId { get; private init; }
    public Guid?   UserId    { get; private init; }
    public string? SessionId { get; private init; }
    /// <summary>JSON-serialised extra data (search query, etc.).</summary>
    public string? Payload   { get; private init; }
    public DateTime CreatedAt { get; private init; }

    private UserEvent() {}

    public static UserEvent Create(
        string  eventType,
        int?    productId = null,
        Guid?   userId    = null,
        string? sessionId = null,
        string? payload   = null) => new()
    {
        EventType = eventType,
        ProductId = productId,
        UserId    = userId,
        SessionId = sessionId,
        Payload   = payload,
        CreatedAt = DateTime.UtcNow,
    };
}

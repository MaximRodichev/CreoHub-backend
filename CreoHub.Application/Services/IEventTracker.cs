namespace CreoHub.Application.Services;

/// <summary>
/// Fire-and-forget event tracker.
/// Implementations MUST be thread-safe and MUST NOT throw.
/// The caller never awaits this — latency-sensitive paths (product page, search) call it synchronously.
/// </summary>
public interface IEventTracker
{
    void Track(
        string  eventType,
        int?    productId = null,
        Guid?   userId    = null,
        string? sessionId = null,
        object? payload   = null);
}

/// <summary>Well-known event type constants.</summary>
public static class EventTypes
{
    public const string ProductView       = "product_view";
    public const string Search            = "search";
    public const string SearchNoResults   = "search_no_results";
    public const string CartAdd           = "cart_add";
    public const string CartRemove        = "cart_remove";
    public const string CheckoutStarted   = "checkout_started";
    public const string CheckoutCompleted = "checkout_completed";
    public const string ProductPurchased  = "product_purchased";
    public const string PageView          = "page_view";
}

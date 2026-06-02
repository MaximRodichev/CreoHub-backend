using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

/// <summary>
/// Закрытое предложение покупателя магазину («хочу увидеть такой-то товар»).
/// Не чат: покупатель отправляет один текст, продавец отвечает или отклоняет.
/// BuyerName / BuyerEmail — снапшот на момент отправки (имя могло потом поменяться).
/// </summary>
public class ShopRequest
{
    public int               Id          { get; private set; }
    public Guid              ShopId      { get; private set; }
    public Guid              BuyerUserId { get; private set; }
    public string            BuyerName   { get; private set; } = string.Empty;
    public string            BuyerEmail  { get; private set; } = string.Empty;
    public string            Message     { get; private set; } = string.Empty;
    public ShopRequestStatus Status      { get; private set; } = ShopRequestStatus.New;
    public string?           SellerReply { get; private set; }
    public DateTime          CreatedAt   { get; private set; }
    public DateTime?         RepliedAt   { get; private set; }

    private ShopRequest() {}

    public static ShopRequest Create(
        Guid shopId, Guid buyerUserId, string? buyerName, string? buyerEmail, string message)
    {
        if (shopId == Guid.Empty)
            throw new ArgumentException("ShopId is required.", nameof(shopId));
        if (buyerUserId == Guid.Empty)
            throw new ArgumentException("BuyerUserId is required.", nameof(buyerUserId));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        return new ShopRequest
        {
            ShopId      = shopId,
            BuyerUserId = buyerUserId,
            BuyerName   = string.IsNullOrWhiteSpace(buyerName) ? "Покупатель" : buyerName.Trim(),
            BuyerEmail  = buyerEmail?.Trim() ?? string.Empty,
            Message     = message.Trim(),
            Status      = ShopRequestStatus.New,
            CreatedAt   = DateTime.UtcNow,
        };
    }

    /// <summary>Продавец отвечает покупателю.</summary>
    public void Reply(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Reply cannot be empty.", nameof(text));
        SellerReply = text.Trim();
        Status      = ShopRequestStatus.Replied;
        RepliedAt   = DateTime.UtcNow;
    }

    /// <summary>Продавец отклоняет предложение. Причина опциональна.</summary>
    public void Decline(string? reason)
    {
        SellerReply = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Status      = ShopRequestStatus.Declined;
        RepliedAt   = DateTime.UtcNow;
    }
}

using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

/// <summary>
/// Постоянное in-app уведомление. Гарантированная доставка:
/// создаётся всегда при важном событии независимо от статуса внешних каналов.
/// </summary>
public class InAppNotification
{
    public int               Id         { get; private set; }
    public Guid              UserId     { get; private set; }
    public NotificationType  Type       { get; private set; }
    public string            Message    { get; private set; } = string.Empty;
    /// <summary>Опциональная ссылка «Перейти к заказу», «Открыть товар» и т.п.</summary>
    public string?           ActionUrl  { get; private set; }
    public DateTime          CreatedAt  { get; private set; }
    public bool              IsRead     { get; private set; }
    public DateTime?         ReadAt     { get; private set; }

    private InAppNotification() {}

    public static InAppNotification Create(
        Guid userId, NotificationType type,
        string message, string? actionUrl = null) =>
        new()
        {
            UserId    = userId,
            Type      = type,
            Message   = message,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow,
            IsRead    = false,
        };

    public void Acknowledge()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}

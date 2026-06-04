namespace CreoHub.Domain.Entities;

/// <summary>
/// Настройки уведомлений пользователя. 1:1 с User.
/// Создаётся при регистрации (eager creation), всегда существует.
/// </summary>
public class UserNotificationSettings
{
    public Guid UserId { get; private set; }

    // ── Каналы доставки ───────────────────────────────────────────────────────
    /// <summary>Разрешить отправку уведомлений в Telegram.</summary>
    public bool TelegramEnabled { get; private set; } = true;
    /// <summary>Разрешить отправку уведомлений на Email.</summary>
    public bool EmailEnabled    { get; private set; } = true;

    // ── Типы событий ──────────────────────────────────────────────────────────
    /// <summary>Уведомлять при новой продаже (продавцу).</summary>
    public bool NotifyOnPurchase    { get; private set; } = true;
    /// <summary>Уведомлять при одобрении / отклонении товара (продавцу).</summary>
    public bool NotifyOnModeration  { get; private set; } = true;
    /// <summary>Уведомлять при пополнении баланса.</summary>
    public bool NotifyOnBalance     { get; private set; } = true;
    /// <summary>Получать рассылки от администрации платформы.</summary>
    public bool NotifyOnBroadcast   { get; private set; } = true;
    /// <summary>Уведомлять о новых товарах в подписанных магазинах.</summary>
    public bool NotifyOnNewProduct  { get; private set; } = true;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private UserNotificationSettings() {}

    public static UserNotificationSettings CreateDefault(Guid userId) =>
        new() { UserId = userId };

    public void Update(
        bool telegramEnabled,
        bool emailEnabled,
        bool notifyOnPurchase,
        bool notifyOnModeration,
        bool notifyOnBalance,
        bool notifyOnBroadcast,
        bool notifyOnNewProduct)
    {
        TelegramEnabled    = telegramEnabled;
        EmailEnabled       = emailEnabled;
        NotifyOnPurchase   = notifyOnPurchase;
        NotifyOnModeration = notifyOnModeration;
        NotifyOnBalance    = notifyOnBalance;
        NotifyOnBroadcast  = notifyOnBroadcast;
        NotifyOnNewProduct = notifyOnNewProduct;
        UpdatedAt          = DateTime.UtcNow;
    }
}

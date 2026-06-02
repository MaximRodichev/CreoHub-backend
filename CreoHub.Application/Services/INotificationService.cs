using CreoHub.Domain.Types;

namespace CreoHub.Application.Services;

/// <summary>
/// Единая точка входа для всех уведомлений.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Постоянное уведомление о системном событии:
    ///   1. Всегда сохраняет в InAppNotifications (гарантированная доставка).
    ///   2. Если telegramChatId != null — шлёт в Telegram.
    ///   3. Если email != null — шлёт на Email.
    /// Caller сам передаёт channel-параметры на основе загруженных настроек юзера.
    /// </summary>
    Task NotifyAsync(
        Guid             userId,
        NotificationType type,
        string           message,
        string?          actionUrl      = null,
        long?            telegramChatId = null,
        string?          email          = null,
        CancellationToken ct            = default);

    /// <summary>
    /// Транзиентное уведомление (только каналы, без сохранения в БД).
    /// Используется для подтверждений, пингов и т.п.
    /// Telegram → Email fallback. Возвращает true если хоть один канал сработал.
    /// </summary>
    Task<bool> SendAsync(
        long?   telegramId,
        string? email,
        string  message,
        CancellationToken ct = default);
}

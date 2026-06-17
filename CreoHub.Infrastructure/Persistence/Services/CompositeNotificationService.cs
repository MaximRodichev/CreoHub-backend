using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreoHub.Infrastructure.Persistence.Services;

/// <summary>
/// INotificationService implementation.
///
/// NotifyAsync  — всегда сохраняет в InAppNotifications, затем пробует Telegram → Email.
/// SendAsync    — только каналы (транзиентные подтверждения, без записи в БД).
/// Все исключения поглощаются — уведомления никогда не должны ломать основной поток.
/// </summary>
public class CompositeNotificationService : INotificationService
{
    private readonly TelegramNotificationService      _telegram;
    private readonly SmtpNotificationService          _smtp;
    private readonly IInAppNotificationRepository     _inApp;
    private readonly ILogger<CompositeNotificationService> _logger;
    private readonly string?                          _frontendBase;

    public CompositeNotificationService(
        TelegramNotificationService          telegram,
        SmtpNotificationService              smtp,
        IInAppNotificationRepository         inApp,
        IConfiguration                       configuration,
        ILogger<CompositeNotificationService> logger)
    {
        _telegram     = telegram;
        _smtp         = smtp;
        _inApp        = inApp;
        _logger       = logger;
        _frontendBase = configuration["Frontend"]?.TrimEnd('/');
    }

    /// <summary>
    /// Превращает actionUrl ("/store/product/x" или абсолютный) в полный URL сайта.
    /// null — если ссылки нет или не задан Frontend base.
    /// </summary>
    private string? BuildAbsoluteUrl(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl)) return null;
        if (actionUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return actionUrl;
        if (string.IsNullOrWhiteSpace(_frontendBase)) return null;
        return $"{_frontendBase}/{actionUrl.TrimStart('/')}";
    }

    // ── Постоянное уведомление ────────────────────────────────────────────────
    public async Task NotifyAsync(
        Guid userId, NotificationType type, string message,
        string? actionUrl = null, long? telegramChatId = null, string? email = null,
        CancellationToken ct = default)
    {
        try
        {
            // 1. In-app — всегда (гарантированная доставка). Хранит относительный actionUrl.
            await _inApp.AddAsync(userId, type, message, actionUrl, ct);

            _logger.LogInformation(
                "CompositeNotification.NotifyAsync: userId={UserId} type={Type} tg={Tg} email={Email}",
                userId, type, telegramChatId, email);

            // Внешним каналам нужна абсолютная ссылка (с доменом сайта)
            var absUrl = BuildAbsoluteUrl(actionUrl);

            // 2. Telegram — HTML-ссылка кликабельна (parse_mode=HTML)
            if (telegramChatId.HasValue)
            {
                var tgText = absUrl is null ? message : $"{message}\n\n<a href=\"{absUrl}\">Перейти →</a>";
                var sent = await _telegram.TrySendAsync(telegramChatId.Value, tgText, ct);
                if (sent)
                {
                    _logger.LogInformation("CompositeNotification: sent via Telegram to {ChatId}", telegramChatId);
                    return;
                }
            }

            // 3. Email fallback — простая ссылка отдельной строкой
            if (!string.IsNullOrWhiteSpace(email))
            {
                const string subject = "CreoHub — уведомление";
                var emailText = absUrl is null ? message : $"{message}\n\n{absUrl}";
                var sent = await _smtp.TrySendAsync(email, subject, emailText, ct);
                if (sent)
                    _logger.LogInformation("CompositeNotification: sent via Email to {Email}", email);
                else
                    _logger.LogWarning("CompositeNotification: email failed for {Email}", email);
            }

            if (!telegramChatId.HasValue && string.IsNullOrWhiteSpace(email))
                _logger.LogWarning("CompositeNotification: no external channel — in-app only for userId={UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CompositeNotification.NotifyAsync: unexpected exception for userId={UserId}", userId);
        }
    }

    // ── Транзиентное уведомление (только каналы, без БД) ─────────────────────
    public async Task<bool> SendAsync(
        long? telegramId, string? email, string message,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "CompositeNotification.SendAsync: telegramId={TelegramId} hasEmail={HasEmail}",
                telegramId, !string.IsNullOrWhiteSpace(email));

            if (telegramId.HasValue)
            {
                var sent = await _telegram.TrySendAsync(telegramId.Value, message, ct);
                if (sent)
                {
                    _logger.LogInformation("CompositeNotification: sent via Telegram to {ChatId}", telegramId);
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                const string subject = "CreoHub — уведомление";
                var sent = await _smtp.TrySendAsync(email, subject, message, ct);
                if (sent)
                {
                    _logger.LogInformation("CompositeNotification: sent via Email to {Email}", email);
                    return true;
                }
            }

            if (!telegramId.HasValue && string.IsNullOrWhiteSpace(email))
                _logger.LogWarning("CompositeNotification: нет ни TelegramId ни Email — уведомление пропущено");
            else
                _logger.LogWarning("CompositeNotification: все каналы недоступны (telegramId={TelegramId}, email={Email})",
                    telegramId, email);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CompositeNotification.SendAsync: unexpected exception");
            return false;
        }
    }
}

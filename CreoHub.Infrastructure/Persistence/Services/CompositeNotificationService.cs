using CreoHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace CreoHub.Infrastructure.Persistence.Services;

/// <summary>
/// INotificationService implementation:
/// 1. Tries Telegram (if telegramId is set)
/// 2. Falls back to email (if email is set)
/// Returns true if at least one channel succeeded.
/// All exceptions are swallowed to protect the main request path.
/// </summary>
public class CompositeNotificationService : INotificationService
{
    private readonly TelegramNotificationService _telegram;
    private readonly SmtpNotificationService     _smtp;
    private readonly ILogger<CompositeNotificationService> _logger;

    public CompositeNotificationService(
        TelegramNotificationService telegram,
        SmtpNotificationService     smtp,
        ILogger<CompositeNotificationService> logger)
    {
        _telegram = telegram;
        _smtp     = smtp;
        _logger   = logger;
    }

    public async Task<bool> SendAsync(
        long?   telegramId,
        string? email,
        string  message,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Telegram (preferred)
            if (telegramId.HasValue)
            {
                var sent = await _telegram.TrySendAsync(telegramId.Value, message, ct);
                if (sent) return true;
            }

            // 2. Email fallback
            if (!string.IsNullOrWhiteSpace(email))
            {
                const string subject = "CreoHub — уведомление";
                var sent = await _smtp.TrySendAsync(email, subject, message, ct);
                if (sent) return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CompositeNotificationService: unexpected exception");
            return false;
        }
    }
}

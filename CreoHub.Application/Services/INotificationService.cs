namespace CreoHub.Application.Services;

/// <summary>
/// Sends a notification to a user via their preferred channel (Telegram → Email fallback).
/// Implementations should be fire-and-forget-safe; all exceptions must be swallowed internally.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Attempt to notify a user.
    /// Tries Telegram first (if telegramId is set), then falls back to email.
    /// Returns true if at least one channel succeeded.
    /// </summary>
    Task<bool> SendAsync(
        long?   telegramId,
        string? email,
        string  message,
        CancellationToken ct = default);
}

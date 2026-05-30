using CreoHub.Application.DTO.AccountDTOs;

namespace CreoHub.Application.Services;

/// <summary>
/// Верифицирует подпись данных от Telegram Login Widget (HMAC-SHA256).
/// </summary>
public interface ITelegramAuthVerifier
{
    /// <summary>
    /// Возвращает true, если данные подписаны корректно и не устарели (не старше 24ч).
    /// </summary>
    bool Verify(TelegramAuthData data);
}

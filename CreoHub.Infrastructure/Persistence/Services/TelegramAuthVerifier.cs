using System.Security.Cryptography;
using System.Text;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Services;
using Microsoft.Extensions.Configuration;

namespace CreoHub.Infrastructure.Persistence.Services;

public class TelegramAuthVerifier : ITelegramAuthVerifier
{
    private readonly byte[] _secretKey;
    private const int MaxAuthAgeSec = 86_400; // 24 часа

    public TelegramAuthVerifier(IConfiguration configuration)
    {
        var botToken = configuration["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Telegram:BotToken не настроен в конфигурации.");

        // Секретный ключ = SHA256(bot_token) согласно алгоритму Telegram
        _secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
    }

    public bool Verify(TelegramAuthData data)
    {
        // 1. Проверяем свежесть auth_date (не старше 24 часов)
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - data.AuthDate > MaxAuthAgeSec)
            return false;

        // 2. Строим data-check-string: все поля кроме hash, отсортированные по ключу, через \n
        var fields = new SortedDictionary<string, string>
        {
            ["id"]         = data.Id.ToString(),
            ["first_name"] = data.FirstName,
            ["auth_date"]  = data.AuthDate.ToString(),
        };
        if (!string.IsNullOrEmpty(data.LastName))  fields["last_name"]  = data.LastName;
        if (!string.IsNullOrEmpty(data.Username))  fields["username"]   = data.Username;
        if (!string.IsNullOrEmpty(data.PhotoUrl))  fields["photo_url"]  = data.PhotoUrl;

        var dataCheckString = string.Join("\n", fields.Select(kv => $"{kv.Key}={kv.Value}"));

        // 3. HMAC-SHA256(data-check-string, SHA256(bot_token))
        using var hmac = new HMACSHA256(_secretKey);
        var computed = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString))
        ).ToLowerInvariant();

        return computed == data.Hash.ToLowerInvariant();
    }
}

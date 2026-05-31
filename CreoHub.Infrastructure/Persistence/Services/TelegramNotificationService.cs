using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreoHub.Infrastructure.Persistence.Services;

/// <summary>
/// Sends a Telegram message via Bot API.
/// Requires configuration key "Telegram:BotToken".
/// </summary>
public class TelegramNotificationService
{
    private readonly HttpClient  _http;
    private readonly string?     _botToken;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(
        HttpClient   httpClient,
        IConfiguration configuration,
        ILogger<TelegramNotificationService> logger)
    {
        _http     = httpClient;
        _botToken = configuration["Telegram:BotToken"];
        _logger   = logger;
    }

    public async Task<bool> TrySendAsync(long chatId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_botToken)) return false;

        try
        {
            var url     = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = JsonSerializer.Serialize(new { chat_id = chatId, text = message, parse_mode = "HTML" });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram notification failed for chat_id={ChatId}", chatId);
            return false;
        }
    }
}

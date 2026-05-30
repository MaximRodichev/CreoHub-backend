using System.Text.Json.Serialization;

namespace CreoHub.Application.DTO.AccountDTOs;

/// <summary>
/// Данные, которые Telegram Login Widget передаёт в JS-колбэк после авторизации.
/// Поля snake_case — именно так Telegram отдаёт их через виджет/API.
/// </summary>
public record TelegramAuthData(
    [property: JsonPropertyName("id")]         long    Id,
    [property: JsonPropertyName("first_name")] string  FirstName,
    [property: JsonPropertyName("auth_date")]  long    AuthDate,
    [property: JsonPropertyName("hash")]       string  Hash,
    [property: JsonPropertyName("last_name")]  string? LastName  = null,
    [property: JsonPropertyName("username")]   string? Username  = null,
    [property: JsonPropertyName("photo_url")]  string? PhotoUrl  = null
);

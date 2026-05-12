using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;

public class OxaPayPayoutResponse
{
    /// <summary>100 = Success, всё остальное — ошибка.</summary>
    [JsonPropertyName("result")]
    public int Result { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public OxaPayPayoutData? Data { get; set; }

    public bool IsSuccess => Result == 100;
}

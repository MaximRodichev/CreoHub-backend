using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;

public class OxaPayInquiryResponse
{
    [JsonPropertyName("result")]
    public int Result { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public OxaPayInquiryData Data { get; set; } = new();
}

public class OxaPayInquiryData
{
    [JsonPropertyName("track_id")]
    public string TrackId { get; set; } = string.Empty;

    [JsonPropertyName("payment_url")]
    public string PaymentUrl { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("expired_at")]
    public long ExpiredAt { get; set; }
}

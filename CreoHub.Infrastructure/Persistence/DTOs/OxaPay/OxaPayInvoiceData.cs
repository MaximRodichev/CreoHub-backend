using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;

public class OxaPayInvoiceData
{
    [JsonPropertyName("track_id")]
    public string TrackId { get; set; }
    
    [JsonPropertyName("payment_url")]
    public string PaymentUrl { get; set; }
    
    [JsonPropertyName("expired_at")]
    public long ExpiredAt { get; set; }
}
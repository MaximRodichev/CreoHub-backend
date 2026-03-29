using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;

public class OxaPayPayoutData
{
    [JsonPropertyName("track_id")]
    public string TrackId { get; set; }
}
using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;


public class OxaPayPayoutResponse
{
    [JsonPropertyName("data")]
    public OxaPayPayoutData Data { get; set; }
}
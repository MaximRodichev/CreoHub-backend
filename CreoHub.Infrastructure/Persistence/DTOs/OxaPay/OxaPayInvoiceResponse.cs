using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;

public class OxaPayInvoiceResponse
{
    [JsonPropertyName("data")]
    public OxaPayInvoiceData Data { get; set; }
    
    [JsonPropertyName("status")]
    public int Status { get; set; }
}

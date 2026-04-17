using System.Text.Json.Serialization;

namespace CreoHub.Infrastructure.Persistence.DTOs.OxaPay;

public class OxaPayWebhookPayload
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    // Важно: в JSON приходит "track_id" (со значком подчеркивания)
    [JsonPropertyName("track_id")]
    public string TrackId { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    // В Sandbox приходит "value" или "sent_value", 
    // иногда полезно иметь их для сверки
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    // Массив транзакций — именно здесь лежат недостающие данные
    [JsonPropertyName("txs")]
    public List<OxaPayTransaction> Transactions { get; set; } = new();
}

public class OxaPayTransaction
{
    [JsonPropertyName("tx_hash")]
    public string? TxHash { get; set; }

    [JsonPropertyName("network")]
    public string? Network { get; set; }

    [JsonPropertyName("sender_address")] // В JSON из примера это поле называется так
    public string? SenderAddress { get; set; }

    [JsonPropertyName("address")] // Адрес, на который пришла оплата
    public string? DestinationAddress { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
namespace CreoHub.Domain.Entities;

/// <summary>
/// Сырой лог каждого входящего вебхука платёжного провайдера (OxaPay).
/// Forensic-журнал для разбора спорных платежей: запрошено/получено, недоплаты,
/// застрявшие средства. Одна транзакция может получить несколько вебхуков
/// (Waiting → Confirming → Paid, повторы при ретраях) — поэтому отдельная таблица.
///
/// Доступа извне (API) нет — смотреть запросом в БД при необходимости.
/// </summary>
public class PaymentWebhookLog
{
    public Guid     Id         { get; private init; } = Guid.NewGuid();
    public string   TrackId    { get; private init; } = string.Empty;
    public string   Status     { get; private init; } = string.Empty;
    public string   RawJson    { get; private init; } = string.Empty;
    public DateTime ReceivedAt { get; private init; } = DateTime.UtcNow;

    private PaymentWebhookLog() {}

    public static PaymentWebhookLog Create(string trackId, string status, string rawJson) =>
        new()
        {
            TrackId    = trackId ?? string.Empty,
            Status     = status  ?? string.Empty,
            RawJson    = rawJson ?? string.Empty,
            ReceivedAt = DateTime.UtcNow,
        };
}

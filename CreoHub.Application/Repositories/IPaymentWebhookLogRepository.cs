namespace CreoHub.Application.Repositories;

/// <summary>
/// Журнал сырых платёжных вебхуков. Сохраняет независимо от основного потока
/// (свой коммит) — лог не должен влиять на обработку платежа и наоборот.
/// </summary>
public interface IPaymentWebhookLogRepository
{
    Task SaveAsync(string trackId, string status, string rawJson, CancellationToken ct = default);
}

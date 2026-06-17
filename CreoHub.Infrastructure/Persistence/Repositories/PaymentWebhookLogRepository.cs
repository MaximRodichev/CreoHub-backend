using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class PaymentWebhookLogRepository : IPaymentWebhookLogRepository
{
    private readonly AppDbContext _db;

    public PaymentWebhookLogRepository(AppDbContext db) => _db = db;

    public async Task SaveAsync(string trackId, string status, string rawJson, CancellationToken ct = default)
    {
        var log = PaymentWebhookLog.Create(trackId, status, rawJson);
        await _db.PaymentWebhookLogs.AddAsync(log, ct);
        await _db.SaveChangesAsync(ct);
    }
}

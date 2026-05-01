using CreoHub.Domain.Types;
using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.API.Services;

/// <summary>
/// Фоновый джоб: каждые 5 минут переводит Pending-транзакции
/// старше их реального времени жизни (lifetime + буфер) в статус Expired.
///
/// Зачем: OxaPay шлёт Expired-вебхук, но он может потеряться
/// (смена ngrok URL, сетевой сбой, таймаут). Джоб — страховка.
/// </summary>
public class ExpiredTransactionCleanupService : BackgroundService
{
    private static readonly TimeSpan CheckInterval   = TimeSpan.FromMinutes(5);
    // lifetime инвойса = 30 мин (в OxaPayService), +10 мин буфер
    private static readonly TimeSpan ExpiryThreshold = TimeSpan.FromMinutes(40);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredTransactionCleanupService> _logger;

    public ExpiredTransactionCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredTransactionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ExpiredTransactionCleanup: started, interval={Interval}, threshold={Threshold}",
            CheckInterval, ExpiryThreshold);

        // Небольшая задержка при старте — даём приложению полностью подняться
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MarkExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExpiredTransactionCleanup: unhandled error");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("ExpiredTransactionCleanup: stopped");
    }

    private async Task MarkExpiredAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - ExpiryThreshold;

        // Загружаем только Pending, старше порога, типов которые создаются через OxaPay
        // (UpBalance и Purchase; Withdrawal — синхронный, Pending не бывает долго)
        var stale = await db.UserTransactions
            .Where(t => t.TransactionStatus == TransactionStatus.Pending
                     && t.CreatedAt < cutoff
                     && (t.TransactionType == TransactionType.UpBalance
                      || t.TransactionType == TransactionType.Purchase))
            .ToListAsync(ct);

        if (stale.Count == 0)
            return;

        _logger.LogInformation(
            "ExpiredTransactionCleanup: marking {Count} transaction(s) as Expired", stale.Count);

        foreach (var tx in stale)
        {
            try
            {
                tx.Expire();
                _logger.LogInformation(
                    "ExpiredTransactionCleanup: Expired trackId={TrackId} type={Type} createdAt={CreatedAt}",
                    tx.TrackId, tx.TransactionType, tx.CreatedAt);
            }
            catch (Exception ex)
            {
                // Может бросить если статус уже не Pending — пропускаем
                _logger.LogWarning(
                    "ExpiredTransactionCleanup: cannot expire trackId={TrackId}: {Error}",
                    tx.TrackId, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

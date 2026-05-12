using CreoHub.Domain.Types;
using CreoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.API.Services;

/// <summary>
/// Фоновый джоб: каждые 15 минут отменяет заказы в статусе Created,
/// которые старше 1 часа (пользователь не оплатил — заказ протух).
///
/// Транзакции при этом НЕ трогаем — их экспайрит ExpiredTransactionCleanupService.
/// Сюда попадают в том числе заказы без транзакций (созданные вручную админом
/// через CreateOrderDev) — они тоже должны отмениться если не были закрыты.
/// </summary>
public sealed class OrderCleanupService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan OrderExpiry   = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCleanupService> _logger;

    public OrderCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OrderCleanupService started — interval={Interval}, expiry={Expiry}",
            CheckInterval, OrderExpiry);

        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelStaleOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderCleanupService: unhandled error");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("OrderCleanupService stopped");
    }

    private async Task CancelStaleOrdersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - OrderExpiry;

        var stale = await db.Orders
            .Where(o => o.Status == OrderStatus.Created && o.OrderDate < cutoff)
            .ToListAsync(ct);

        if (stale.Count == 0) return;

        foreach (var order in stale)
        {
            try
            {
                order.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "OrderCleanupService: cannot cancel orderId={OrderId}: {Error}",
                    order.Id, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "OrderCleanupService: cancelled {Count} stale order(s)", stale.Count);
    }
}

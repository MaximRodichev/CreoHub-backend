using System.Threading.Channels;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;

namespace CreoHub.API.Services;

/// <summary>
/// Drains the UserEvent channel and writes events to the DB in batches.
/// Flushes every <see cref="FlushSeconds"/> seconds OR when batch reaches <see cref="BatchSize"/>.
/// Scoped DbContext is created per flush so EF Core's change-tracker stays clean.
/// </summary>
public class EventTrackerBackgroundService : BackgroundService
{
    private readonly Channel<UserEvent>                    _channel;
    private readonly IServiceScopeFactory                  _scopeFactory;
    private readonly ILogger<EventTrackerBackgroundService> _logger;

    private const int BatchSize    = 100;
    private const int FlushSeconds = 5;

    public EventTrackerBackgroundService(
        Channel<UserEvent> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EventTrackerBackgroundService> logger)
    {
        _channel      = channel;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<UserEvent>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait up to FlushSeconds for events, collecting up to BatchSize
            try
            {
                using var flushTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(FlushSeconds));
                using var linked       = CancellationTokenSource.CreateLinkedTokenSource(
                    stoppingToken, flushTimeout.Token);

                while (batch.Count < BatchSize)
                {
                    // Blocks until at least one item is available or token fires
                    await _channel.Reader.WaitToReadAsync(linked.Token);

                    // Drain everything currently in the channel
                    while (batch.Count < BatchSize && _channel.Reader.TryRead(out var ev))
                        batch.Add(ev);

                    if (batch.Count >= BatchSize) break;
                }
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Flush timer expired — that's fine, fall through to flush
            }

            if (batch.Count > 0)
            {
                await FlushAsync(batch, stoppingToken);
                batch.Clear();
            }
        }

        // ── Graceful shutdown: flush whatever's left ──────────────────────────
        while (_channel.Reader.TryRead(out var remaining))
            batch.Add(remaining);

        if (batch.Count > 0)
            await FlushAsync(batch, CancellationToken.None);
    }

    private async Task FlushAsync(List<UserEvent> batch, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IUserEventRepository>();
            await repo.BulkInsertAsync(batch, ct);
            _logger.LogDebug("EventTracker: flushed {Count} events", batch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventTracker: failed to flush {Count} events", batch.Count);
            // Don't rethrow — losing a batch of analytics beats crashing the service
        }
    }
}

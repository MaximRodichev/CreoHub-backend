using CreoHub.Application.Repositories;
using CreoHub.Application.Services;

namespace CreoHub.API.Services;

/// <summary>
/// Ночной джоб (03:00 UTC): генерирует thumbnail-превью для всех видео на платформе,
/// у которых thumbnail ещё не создан.
///
/// Сценарий: видео загружено без оптимизации (не через очередь), или джоб thumbnail-генерации
/// завершился с ошибкой при первом запуске.
/// </summary>
public class ThumbnailBackfillBackgroundService : BackgroundService
{
    private static readonly TimeSpan RunAtHour = TimeSpan.FromHours(3); // 03:00 UTC

    private readonly IServiceScopeFactory                       _scopeFactory;
    private readonly ILogger<ThumbnailBackfillBackgroundService> _logger;

    public ThumbnailBackfillBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ThumbnailBackfillBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ThumbnailBackfill: started, runs daily at 03:00 UTC");

        // Небольшая задержка при старте — даём платформе подняться
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            _logger.LogDebug("ThumbnailBackfill: next run in {Delay}", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            try
            {
                await RunBackfillAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ThumbnailBackfill: unhandled error");
            }
        }

        _logger.LogInformation("ThumbnailBackfill: stopped");
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var now      = DateTime.UtcNow;
        var todayRun = now.Date + RunAtHour;
        var nextRun  = now < todayRun ? todayRun : todayRun.AddDays(1);
        return nextRun - now;
    }

    private async Task RunBackfillAsync(CancellationToken ct)
    {
        _logger.LogInformation("ThumbnailBackfill: starting nightly run");

        using var scope       = _scopeFactory.CreateScope();
        var mediaRepo         = scope.ServiceProvider.GetRequiredService<IMediaProductRepository>();
        var thumbnailService  = scope.ServiceProvider.GetRequiredService<IThumbnailGenerationService>();

        var candidates = await mediaRepo.GetAllVideosWithoutThumbnailAsync();
        _logger.LogInformation("ThumbnailBackfill: {Count} videos need thumbnails", candidates.Count);

        int succeeded = 0;
        int failed    = 0;

        foreach (var mp in candidates)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await thumbnailService.GenerateAsync(mp.StorageObjectId, ct);
                succeeded++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(
                    "ThumbnailBackfill: failed for StorageObject {Id}: {Error}",
                    mp.StorageObjectId, ex.Message);
            }
        }

        _logger.LogInformation(
            "ThumbnailBackfill: nightly run complete — succeeded={Succeeded}, failed={Failed}",
            succeeded, failed);
    }
}

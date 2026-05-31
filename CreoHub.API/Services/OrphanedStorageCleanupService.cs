using CreoHub.Application.Repositories;
using CreoHub.Application.Services;

namespace CreoHub.API.Services;

/// <summary>
/// Ежедневный джоб (03:30 UTC): удаляет файлы в R2, которые не зарегистрированы в БД
/// и существуют дольше 2 часов (защита от race-condition при загрузке).
///
/// Сценарий: пользователь начал presigned-upload но не дошёл до /s3/confirm-upload.
/// Файл оказался в R2, записи в БД нет. Джоб подчищает такой мусор раз в сутки.
/// </summary>
public class OrphanedStorageCleanupService : BackgroundService
{
    private static readonly TimeSpan OrphanAge   = TimeSpan.FromHours(2);
    private static readonly TimeSpan RunAtHour   = TimeSpan.FromHours(3.5); // 03:30 UTC

    private readonly IServiceScopeFactory                   _scopeFactory;
    private readonly ILogger<OrphanedStorageCleanupService> _logger;

    public OrphanedStorageCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrphanedStorageCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrphanedStorageCleanup: started, runs daily at 03:30 UTC");

        // Небольшая задержка при старте
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextRun();
            _logger.LogDebug("OrphanedStorageCleanup: next run in {Delay}", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrphanedStorageCleanup: unhandled error");
            }
        }

        _logger.LogInformation("OrphanedStorageCleanup: stopped");
    }

    private static TimeSpan TimeUntilNextRun()
    {
        var now         = DateTime.UtcNow;
        var todayRun    = now.Date + RunAtHour;
        var nextRun     = now < todayRun ? todayRun : todayRun.AddDays(1);
        return nextRun - now;
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation("OrphanedStorageCleanup: starting run");

        using var scope     = _scopeFactory.CreateScope();
        var storageService  = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var storageRepo     = scope.ServiceProvider.GetRequiredService<IStorageObjectRepository>();

        // 1. Загрузить все ключи из БД (HashSet для O(1) lookup)
        var dbKeys = await storageRepo.GetAllKeysSetAsync(ct);
        _logger.LogInformation("OrphanedStorageCleanup: {Count} keys in DB", dbKeys.Count);

        var cutoff  = DateTime.UtcNow - OrphanAge;
        int deleted = 0;
        int errors  = 0;

        // 2. Перебрать все объекты в R2
        await foreach (var (key, lastModified) in storageService.ListAllObjectsAsync().WithCancellation(ct))
        {
            // Пропускаем если: есть в БД ИЛИ файл создан менее OrphanAge назад
            if (dbKeys.Contains(key) || lastModified > cutoff)
                continue;

            try
            {
                var ok = await storageService.DeleteFileAsync(key);
                if (ok)
                {
                    deleted++;
                    _logger.LogInformation("OrphanedStorageCleanup: deleted orphan {Key}", key);
                }
                else
                {
                    _logger.LogWarning("OrphanedStorageCleanup: delete returned false for {Key}", key);
                    errors++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrphanedStorageCleanup: error deleting {Key}", key);
                errors++;
            }
        }

        _logger.LogInformation(
            "OrphanedStorageCleanup: run complete — deleted={Deleted}, errors={Errors}",
            deleted, errors);
    }
}

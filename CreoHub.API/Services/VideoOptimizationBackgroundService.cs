using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using Microsoft.Extensions.Logging;

namespace CreoHub.API.Services;

public class VideoOptimizationBackgroundService : BackgroundService
{
    private readonly IVideoOptimizationQueueService _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoOptimizationBackgroundService> _logger;
    private readonly IOptimizationProgressService _progressService;

    // Гарантирует что FFmpeg запускается строго по одному процессу.
    // Очередь уже последовательная, но SemaphoreSlim — защита на случай будущих изменений.
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public VideoOptimizationBackgroundService(
        IVideoOptimizationQueueService queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VideoOptimizationBackgroundService> logger,
        IOptimizationProgressService progressService)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _progressService = progressService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ── Восстановление после рестарта ────────────────────────────────────
        // In-memory очередь теряется при перезапуске сервера. Файлы, у которых
        // в БД остался статус Queued или Processing, ставим заново в очередь.
        await RecoverStuckJobsAsync(stoppingToken);

        await foreach (var storageObjectId in _queue.DequeueAllAsync(stoppingToken))
        {
            await _semaphore.WaitAsync(stoppingToken);
            try
            {
                await ProcessOneAsync(storageObjectId, stoppingToken);
            }
            finally
            {
                _semaphore.Release();
                _queue.Remove(storageObjectId);   // убираем из in-memory set
            }
        }
    }

    private async Task RecoverStuckJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IStorageObjectRepository>();
            var uow  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var stuck = await repo.GetStuckOptimizationJobsAsync(stoppingToken);
            if (stuck.Count == 0) return;

            _logger.LogWarning(
                "VideoOptimization: found {Count} stuck job(s) on startup (Queued/Processing), re-queuing",
                stuck.Count);

            // Processing → Queued: сбрасываем промежуточный статус до чистого Queued
            foreach (var obj in stuck)
            {
                if (obj.VideoOptimizationStatus == VideoOptimizationStatus.Processing)
                    obj.MarkQueued();
            }
            await uow.SaveChangesAsync(stoppingToken);

            foreach (var obj in stuck)
                _queue.TryEnqueue(obj.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VideoOptimization: error during startup recovery");
        }
    }

    private async Task ProcessOneAsync(Guid storageObjectId, CancellationToken stoppingToken)
    {
        // Каждая фаза использует отдельный скоп (→ свой DbContext).
        // Причина: GetByIdAsync использует AsNoTracking(), поэтому Update() каждый раз
        // пытается прикрепить новый экземпляр. Если старый экземпляр из предыдущей фазы
        // ещё tracked в том же DbContext — EF бросает InvalidOperationException (identity conflict).

        try
        {
            // ── Фаза 1: Статус → Processing ─────────────────────────────
            using (var phase1 = _scopeFactory.CreateScope())
            {
                var repo = phase1.ServiceProvider.GetRequiredService<IStorageObjectRepository>();
                var uow  = phase1.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var obj = await repo.GetByIdAsync(storageObjectId);
                if (obj == null)
                {
                    _logger.LogWarning("VideoOptimization: StorageObject {Id} not found, skipping", storageObjectId);
                    return;
                }
                obj.MarkProcessing();
                repo.Update(obj);
                await uow.SaveChangesAsync(stoppingToken);
            }

            // ── Фаза 2: H.264 конвертация ────────────────────────────────
            using (var phase2 = _scopeFactory.CreateScope())
            {
                var conversionService = phase2.ServiceProvider.GetRequiredService<IVideoConversionService>();
                await conversionService.ConvertAsync(storageObjectId, stoppingToken);
            }

            // ── Фаза 3: Thumbnail (кадр на 1-й секунде → jpg) ───────────
            using (var phase3 = _scopeFactory.CreateScope())
            {
                _progressService.SetProgress(storageObjectId, 96);
                var thumbnailService = phase3.ServiceProvider.GetRequiredService<IThumbnailGenerationService>();
                await thumbnailService.GenerateAsync(storageObjectId, stoppingToken);
            }

            // ── Фаза 4: Статус → Done ────────────────────────────────────
            using (var phase4 = _scopeFactory.CreateScope())
            {
                var repo = phase4.ServiceProvider.GetRequiredService<IStorageObjectRepository>();
                var uow  = phase4.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var obj = await repo.GetByIdAsync(storageObjectId);
                if (obj != null)
                {
                    obj.MarkOptimizationDone();
                    repo.Update(obj);
                    await uow.SaveChangesAsync(stoppingToken);
                }
            }

            _progressService.SetProgress(storageObjectId, 100);
            _logger.LogInformation("VideoOptimization: done for {Id}", storageObjectId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Сервер остановлен — не помечаем как Failed, пусть переобработается после рестарта
            _logger.LogWarning("VideoOptimization: cancelled (shutdown) for {Id}", storageObjectId);
            throw;
        }
        catch (Exception ex)
        {
            var isOom = ex is OutOfMemoryException
                     || ex.Message.Contains("Cannot allocate memory", StringComparison.OrdinalIgnoreCase)
                     || ex.Message.Contains("out of memory", StringComparison.OrdinalIgnoreCase);

            if (isOom)
                _logger.LogCritical(ex, "VideoOptimization: OOM — FFmpeg killed by OS for {Id}. " +
                    "Check server memory limits. Error: {Error}", storageObjectId, ex.Message);
            else
                _logger.LogError(ex, "VideoOptimization: failed for {Id}. Error: {Error}", storageObjectId, ex.Message);

            // Статус → Failed (best-effort, создаём новый scope на случай если старый уже испорчен)
            _progressService.SetProgress(storageObjectId, -1);

            try
            {
                using var errScope = _scopeFactory.CreateScope();
                var errRepo = errScope.ServiceProvider.GetRequiredService<IStorageObjectRepository>();
                var errUow  = errScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var errObj  = await errRepo.GetByIdAsync(storageObjectId);
                if (errObj != null)
                {
                    errObj.MarkOptimizationFailed();
                    errRepo.Update(errObj);
                    await errUow.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (Exception markEx)
            {
                _logger.LogError(markEx, "VideoOptimization: failed to mark status Failed for {Id}", storageObjectId);
            }
        }
    }
}

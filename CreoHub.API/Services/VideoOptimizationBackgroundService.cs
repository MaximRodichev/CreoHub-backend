using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace CreoHub.API.Services;

public class VideoOptimizationBackgroundService : BackgroundService
{
    private readonly IVideoOptimizationQueueService _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoOptimizationBackgroundService> _logger;

    // Гарантирует что FFmpeg запускается строго по одному процессу.
    // Очередь уже последовательная, но SemaphoreSlim — защита на случай будущих изменений.
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public VideoOptimizationBackgroundService(
        IVideoOptimizationQueueService queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VideoOptimizationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

    private async Task ProcessOneAsync(Guid storageObjectId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStorageObjectRepository>();
        var uow  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Статус → Processing
            var obj = await repo.GetByIdAsync(storageObjectId);
            if (obj == null)
            {
                _logger.LogWarning("VideoOptimization: StorageObject {Id} not found, skipping", storageObjectId);
                return;
            }
            obj.MarkProcessing();
            repo.Update(obj);
            await uow.SaveChangesAsync(stoppingToken);

            // 1. H.264 конвертация
            var conversionService = scope.ServiceProvider.GetRequiredService<IVideoConversionService>();
            await conversionService.ConvertAsync(storageObjectId, stoppingToken);

            // 2. Thumbnail (кадр на 1-й секунде → jpg)
            var thumbnailService = scope.ServiceProvider.GetRequiredService<IThumbnailGenerationService>();
            await thumbnailService.GenerateAsync(storageObjectId, stoppingToken);

            // Статус → Done
            obj = await repo.GetByIdAsync(storageObjectId);
            if (obj != null)
            {
                obj.MarkOptimizationDone();
                repo.Update(obj);
                await uow.SaveChangesAsync(stoppingToken);
            }

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

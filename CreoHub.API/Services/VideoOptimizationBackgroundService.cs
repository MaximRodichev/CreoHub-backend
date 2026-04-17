using CreoHub.Application.Services;

namespace CreoHub.API.Services;

public class VideoOptimizationBackgroundService : BackgroundService
{
    private readonly IVideoOptimizationQueueService _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public VideoOptimizationBackgroundService(
        IVideoOptimizationQueueService queue, 
        IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var storageObjectId in _queue.DequeueAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();

            // 1. Конвертируем mp4 → webm
            var conversionService = scope.ServiceProvider
                .GetRequiredService<IVideoConversionService>();
            await conversionService.ConvertAsync(storageObjectId, stoppingToken);

            // 2. Генерируем thumbnail из webm (кадр на 1-й секунде → jpg)
            var thumbnailService = scope.ServiceProvider
                .GetRequiredService<IThumbnailGenerationService>();
            await thumbnailService.GenerateAsync(storageObjectId, stoppingToken);
        }
    }
}
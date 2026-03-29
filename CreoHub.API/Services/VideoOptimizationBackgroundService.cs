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
            var handler = scope.ServiceProvider
                .GetRequiredService<IVideoConversionService>();
            
            await handler.ConvertAsync(storageObjectId, stoppingToken);
        }
    }
}
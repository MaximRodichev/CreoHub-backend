using System.Threading.Channels;
using CreoHub.Application.Services;

namespace CreoHub.Infrastructure.Persistence.Services;

public class VideoOptimizationQueueService : IVideoOptimizationQueueService
{
    private readonly Channel<Guid> _channel;
    
    public VideoOptimizationQueueService(Channel<Guid> channel)
    {
        _channel = channel;
    }

    public void Enqueue(Guid storageObjectId)
    {
        _channel.Writer.TryWrite(storageObjectId);
    }

    public async IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
    {
        await foreach (var id in _channel.Reader.ReadAllAsync(cancellationToken))
            yield return id;
    }
}
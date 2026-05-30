using System.Threading.Channels;
using CreoHub.Application.Services;

namespace CreoHub.Infrastructure.Persistence.Services;

public class VideoOptimizationQueueService : IVideoOptimizationQueueService
{
    private readonly Channel<Guid> _channel;

    // In-memory set для дедупликации: один файл не попадёт в очередь дважды.
    // Belt-and-suspenders поверх DB-проверки в OptimizeStorageObjectHandler.
    private readonly HashSet<Guid> _activeIds = new();
    private readonly object _lock = new();

    public VideoOptimizationQueueService(Channel<Guid> channel)
    {
        _channel = channel;
    }

    public bool TryEnqueue(Guid storageObjectId)
    {
        lock (_lock)
        {
            if (!_activeIds.Add(storageObjectId))
                return false; // уже в очереди или обрабатывается
        }
        _channel.Writer.TryWrite(storageObjectId);
        return true;
    }

    public void Remove(Guid storageObjectId)
    {
        lock (_lock)
        {
            _activeIds.Remove(storageObjectId);
        }
    }

    public async IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
    {
        await foreach (var id in _channel.Reader.ReadAllAsync(cancellationToken))
            yield return id;
    }
}

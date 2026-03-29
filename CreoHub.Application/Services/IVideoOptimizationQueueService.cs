namespace CreoHub.Application.Services;

public interface IVideoOptimizationQueueService
{
    void Enqueue(Guid storageObjectId);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
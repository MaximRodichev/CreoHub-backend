namespace CreoHub.Application.Services;

public interface IVideoOptimizationQueueService
{
    /// <summary>
    /// Ставит задачу в очередь. Возвращает false если ID уже присутствует в очереди
    /// (дедупликация — один файл не будет обрабатываться дважды одновременно).
    /// </summary>
    bool TryEnqueue(Guid storageObjectId);

    /// <summary>
    /// Удаляет ID из in-memory set после завершения обработки.
    /// Вызывается из VideoOptimizationBackgroundService в finally-блоке.
    /// </summary>
    void Remove(Guid storageObjectId);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
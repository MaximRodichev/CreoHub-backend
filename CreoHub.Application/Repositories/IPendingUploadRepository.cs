using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IPendingUploadRepository
{
    Task AddAsync(PendingUpload pending, CancellationToken ct = default);
    /// <summary>Найти и удалить запись по key + shopId. Возвращает null если не найдена или просрочена.</summary>
    Task<PendingUpload?> ConsumeAsync(string key, Guid shopId, CancellationToken ct = default);
    /// <summary>Очистка просроченных записей (вызывается фоновым сервисом).</summary>
    Task DeleteExpiredAsync(CancellationToken ct = default);
}

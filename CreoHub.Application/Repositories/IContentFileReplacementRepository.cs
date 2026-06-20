using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IContentFileReplacementRepository
{
    Task<ContentFileReplacement> AddAsync(ContentFileReplacement entity, CancellationToken ct = default);
    Task<ContentFileReplacement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Есть ли уже незакрытая (Pending) заявка на замену для этого контент-файла.</summary>
    Task<bool> HasPendingAsync(Guid contentFileId, CancellationToken ct = default);
    /// <summary>Все ожидающие проверки заявки (для очереди модерации).</summary>
    Task<List<ContentFileReplacement>> GetPendingAsync(CancellationToken ct = default);
}

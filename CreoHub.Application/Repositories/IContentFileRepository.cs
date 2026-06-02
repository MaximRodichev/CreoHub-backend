using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IContentFileRepository : IRepository<ContentFile, Guid>
{
    Task<List<ContentFile>> GetByProductIdAsync(int productId);
    Task<List<ContentFile>> GetByStorageObjectIdAsync(Guid contentFileId);
    public Task<List<ContentFile>> GetByProductIdsAsync(IEnumerable<int> productIds);
    Task<ContentFile?> GetByIdWithStorageAsync(Guid id);
    Task<bool> HasPurchasesAsync(Guid contentFileId);
    Task<int> GetAccessCountAsync(Guid contentFileId);
    /// <summary>Количество активных (не архивированных) контент-файлов у товара.</summary>
    Task<int> GetActiveCountByProductIdAsync(int productId);
    /// <summary>Возвращает Id всех контент-файлов для данного товара (для валидации корзины).</summary>
    Task<HashSet<Guid>> GetIdsByProductIdAsync(int productId, CancellationToken ct = default);
}

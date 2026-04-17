using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IContentFileRepository : IRepository<ContentFile, Guid>
{
    Task<List<ContentFile>> GetByProductIdAsync(int productId);
    Task<List<ContentFile>> GetByStorageObjectIdAsync(Guid contentFileId);
    public Task<List<ContentFile>> GetByProductIdsAsync(IEnumerable<int> productIds);
    Task<ContentFile?> GetByIdWithStorageAsync(Guid id);
}

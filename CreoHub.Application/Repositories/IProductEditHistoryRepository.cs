using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IProductEditHistoryRepository
{
    Task AddAsync(ProductEditHistory entry, CancellationToken ct = default);
    Task<List<ProductEditHistory>> GetByProductIdAsync(int productId, int limit = 10, CancellationToken ct = default);
}

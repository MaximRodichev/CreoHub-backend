using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IProductStatusLogRepository
{
    Task AddAsync(ProductStatusLog log, CancellationToken ct = default);
    Task<List<ProductStatusLog>> GetByProductIdAsync(int productId, CancellationToken ct = default);
}

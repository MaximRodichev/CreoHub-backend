using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ProductStatusLogRepository : IProductStatusLogRepository
{
    private readonly AppDbContext _db;

    public ProductStatusLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ProductStatusLog log, CancellationToken ct = default)
    {
        await _db.ProductStatusLogs.AddAsync(log, ct);
    }

    public async Task<List<ProductStatusLog>> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        return await _db.ProductStatusLogs
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}

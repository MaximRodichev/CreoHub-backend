using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ProductEditHistoryRepository : IProductEditHistoryRepository
{
    private readonly AppDbContext _db;

    public ProductEditHistoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ProductEditHistory entry, CancellationToken ct = default)
    {
        await _db.ProductEditHistories.AddAsync(entry, ct);
    }

    public async Task<List<ProductEditHistory>> GetByProductIdAsync(
        int productId, int limit = 10, CancellationToken ct = default)
    {
        return await _db.ProductEditHistories
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}

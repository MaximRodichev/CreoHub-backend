using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ShopBalanceRepository : IShopBalanceRepository
{
    private readonly AppDbContext _db;

    public ShopBalanceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShopBalance?> GetByIdAsync(Guid id)
    {
        return await _db.ShopBalances.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<ShopBalance>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.ShopBalances
            .Where(b => rangeKeys.Contains(b.Id))
            .ToListAsync();
    }

    public async Task<List<ShopBalance>> GetAllAsync()
    {
        return await _db.ShopBalances.ToListAsync();
    }

    public async Task<ShopBalance> AddAsync(ShopBalance entity)
    {
        return (await _db.ShopBalances.AddAsync(entity)).Entity;
    }

    public void Remove(ShopBalance entity)
    {
        _db.ShopBalances.Remove(entity);
    }

    public ShopBalance Update(ShopBalance entity)
    {
        return _db.ShopBalances.Update(entity).Entity;
    }

    public ShopBalance Attach(ShopBalance entity)
    {
        return _db.ShopBalances.Attach(entity).Entity;
    }

    public async Task<ShopBalance?> GetByShopIdAsync(Guid shopId)
    {
        return await _db.ShopBalances.FirstOrDefaultAsync(b => b.ShopId == shopId);
    }
}

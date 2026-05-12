using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ShopTransactionRepository : IShopTransactionRepository
{
    private readonly AppDbContext _db;

    public ShopTransactionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShopTransaction?> GetByIdAsync(Guid id)
    {
        return await _db.ShopTransactions.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<ShopTransaction>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.ShopTransactions
            .Where(t => rangeKeys.Contains(t.Id))
            .ToListAsync();
    }

    public async Task<List<ShopTransaction>> GetAllAsync()
    {
        return await _db.ShopTransactions.ToListAsync();
    }

    public async Task<ShopTransaction> AddAsync(ShopTransaction entity)
    {
        return (await _db.ShopTransactions.AddAsync(entity)).Entity;
    }

    public void Remove(ShopTransaction entity)
    {
        _db.ShopTransactions.Remove(entity);
    }

    public ShopTransaction Update(ShopTransaction entity)
    {
        return _db.ShopTransactions.Update(entity).Entity;
    }

    public ShopTransaction Attach(ShopTransaction entity)
    {
        return _db.ShopTransactions.Attach(entity).Entity;
    }

    public async Task<ShopTransaction?> GetByTrackIdAsync(string trackId)
    {
        return await _db.ShopTransactions.FirstOrDefaultAsync(t => t.TrackId == trackId);
    }

    public async Task<List<ShopTransaction>> GetByShopIdAsync(Guid shopId)
    {
        return await _db.ShopTransactions
            .Where(t => t.ShopId == shopId)
            .ToListAsync();
    }

    public async Task<ShopTransaction?> GetLastWithdrawalAsync(Guid shopId)
    {
        return await _db.ShopTransactions
            .Where(t => t.ShopId == shopId && t.TransactionType == TransactionType.Withdrawal)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

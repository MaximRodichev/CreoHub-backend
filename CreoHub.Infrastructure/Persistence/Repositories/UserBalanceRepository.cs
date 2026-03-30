using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class UserBalanceRepository : IUserBalanceRepository
{
    private readonly AppDbContext _db;

    public UserBalanceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserBalance?> GetByIdAsync(Guid id)
    {
        return await _db.UserBalances.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<UserBalance>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.UserBalances
            .Where(b => rangeKeys.Contains(b.Id))
            .ToListAsync();
    }

    public async Task<List<UserBalance>> GetAllAsync()
    {
        return await _db.UserBalances.ToListAsync();
    }

    public async Task<UserBalance> AddAsync(UserBalance entity)
    {
        return (await _db.UserBalances.AddAsync(entity)).Entity;
    }

    public void Remove(UserBalance entity)
    {
        _db.UserBalances.Remove(entity);
    }

    public UserBalance Update(UserBalance entity)
    {
        return _db.UserBalances.Update(entity).Entity;
    }

    public UserBalance Attach(UserBalance entity)
    {
        return _db.UserBalances.Attach(entity).Entity;
    }

    public async Task<UserBalance?> GetByUserIdAsync(Guid userId)
    {
        return await _db.UserBalances.FirstOrDefaultAsync(b => b.UserId == userId);
    }
}

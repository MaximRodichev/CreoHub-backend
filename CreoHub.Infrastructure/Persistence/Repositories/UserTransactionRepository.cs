using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class UserTransactionRepository : IUserTransactionRepository
{
    private readonly AppDbContext _db;

    public UserTransactionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserTransaction?> GetByIdAsync(Guid id)
    {
        return await _db.UserTransactions.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<UserTransaction>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.UserTransactions
            .Where(t => rangeKeys.Contains(t.Id))
            .ToListAsync();
    }

    public async Task<List<UserTransaction>> GetAllAsync()
    {
        return await _db.UserTransactions.ToListAsync();
    }

    public async Task<UserTransaction> AddAsync(UserTransaction entity)
    {
        return (await _db.UserTransactions.AddAsync(entity)).Entity;
    }

    public void Remove(UserTransaction entity)
    {
        _db.UserTransactions.Remove(entity);
    }

    public UserTransaction Update(UserTransaction entity)
    {
        return _db.UserTransactions.Update(entity).Entity;
    }

    public UserTransaction Attach(UserTransaction entity)
    {
        return _db.UserTransactions.Attach(entity).Entity;
    }

    public async Task<UserTransaction?> GetByTrackIdAsync(string trackId)
    {
        return await _db.UserTransactions.FirstOrDefaultAsync(t => t.TrackId == trackId);
    }

    public async Task<List<UserTransaction>> GetByUserIdAsync(Guid userId)
    {
        return await _db.UserTransactions
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }
}

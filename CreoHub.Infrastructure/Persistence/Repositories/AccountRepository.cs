using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;

    public AccountRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
       return await _db.Users.FirstAsync(x=>x.Id == id);
    }

    public Task<List<User>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public Task<List<User>> GetAllAsync()
    {
        return  _db.Users.ToListAsync();
    }

    public async Task<User> AddAsync(User entity)
    {
        return (await _db.Users.AddAsync(entity)).Entity;
    }

    public void Remove(User entity)
    {
        throw new NotImplementedException();
    }

    public Task<User> UpdateAsync(User entity)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> GetShopByUserId(Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<User?> FindUserByCredentials(string? email = null, long? telegramId = null)
    {
        if (!string.IsNullOrEmpty(email))
            return await _db.Users.FirstOrDefaultAsync(x => x.EmailAddress == email);

        if (telegramId.HasValue)
            return await _db.Users.FirstOrDefaultAsync(x => x.TelegramId == telegramId.Value);

        return null;
    }

    public Task<User?> GetFullInfoByIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}
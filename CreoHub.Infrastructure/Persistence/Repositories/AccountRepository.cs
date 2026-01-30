using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.DTO.ShopDTOs;
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

    public User Update(User entity)
    {
        return _db.Users.Update(entity).Entity;
        
    }

    public Task<Guid> GetShopByUserId(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<UserProfileDTO> GetUserProfileByUserId(Guid userId)
    {
        return _db.Users
            .Include(x=>x.Shop)
            .Select(a => new UserProfileDTO
        {
            Name =  a.Name,
            Email = a.EmailAddress,
            Id = a.Id,
            shopId = a.Shop.Id,
            shopName = a.Shop.Name,
            TelegramId = a.TelegramId,
            TelegramUsername = a.TelegramUsername,
            RegistrationDate =  a.RegistrationDate,
            Role=a.Role.ToString()
        }).FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<User?> FindUserByCredentials(string? email = null, long? telegramId = null)
    {
        if (!string.IsNullOrEmpty(email))
            return await _db.Users.FirstOrDefaultAsync(x => x.EmailAddress == email);

        if (telegramId.HasValue)
            return await _db.Users.FirstOrDefaultAsync(x => x.TelegramId == telegramId.Value);

        return null;
    }

    public async Task<List<ClientShortInfoDTO>> GetClientsShortInfoAsync(Guid shopId)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Orders.Any(o => o.Product.OwnerId == shopId))
            .Select(u => new 
            {
                User = u,
                ShopOrders = u.Orders.Where(o => o.Product.OwnerId == shopId)
            })
            .Select(x => new ClientShortInfoDTO
            {
                Id = x.User.Id,
                Name = x.User.Name,
                TelegramUsername = x.User.TelegramUsername,
                TotalBuys = x.ShopOrders.Count(),
                TotalSpent = x.ShopOrders.Sum(o => o.Price)
            })
            .ToListAsync();
    }

    public Task<User?> GetFullInfoByIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}
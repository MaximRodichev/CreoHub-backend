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

    public User Attach(User entity)
    {
        return _db.Users.Attach(entity).Entity;
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
    //.Where(u => u.Orders.Any(o => o.Items.Any(x=> x.Product.OwnerId == shopId)))
    public async Task<List<ClientShortInfoDTO>> GetClientsShortInfoAsync(Guid shopId)
    {
        return await _db.Users
            .AsNoTracking()
            // 1. Фильтруем пользователей, у которых есть хотя бы один заказ с товарами этого магазина
            //.Where(u => u.Orders.Any(o => o.Items.Any(i => i.Product.OwnerId == shopId)))
            .Select(u => new 
            {
                u.Id,
                u.Name,
                u.TelegramUsername,
                // 2. Берем только те заказы пользователя, которые относятся к данному магазину
                ShopOrders = u.Orders.Where(o => o.Items.Any(i => i.Product.OwnerId == shopId))
            })
            .Select(x => new ClientShortInfoDTO
            {
                Id = x.Id,
                Name = x.Name,
                TelegramUsername = x.TelegramUsername,
                TotalBuys = x.ShopOrders.Count(),
                // 3. Считаем сумму: Сумма (Количество * Цена) для всех позиций в заказах этого магазина
                TotalSpent = x.ShopOrders.Sum(x=>x.Price)
            })
            .OrderByDescending(x=>x.TotalSpent)
            .ToListAsync();
    }

    public Task<User?> GetFullInfoByIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}
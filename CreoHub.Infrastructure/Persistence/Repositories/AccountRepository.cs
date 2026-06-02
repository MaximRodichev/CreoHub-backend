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
       return await _db.Users.FirstOrDefaultAsync(x=>x.Id == id);
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

    public async Task<UserProfileDTO> GetUserProfileByUserId(Guid userId)
    {
        var user = await _db.Users
            .Include(x => x.Shop)
            .Include(x => x.NotificationSettings)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null) return null;

        var ns = user.NotificationSettings;
        return new UserProfileDTO
        {
            Id                      = user.Id,
            Name                    = user.Name,
            Email                   = user.EmailAddress,
            shopId                  = user.Shop?.Id,
            shopName                = user.Shop?.Name,
            TelegramId              = user.TelegramId,
            TelegramUsername        = user.TelegramUsername,
            RegistrationDate        = user.RegistrationDate,
            Role                    = user.Role.ToString(),
            LifetimeSpent           = user.LifetimeSpent,
            LifetimeDiscountPercent = user.GetLifetimeDiscount() * 100m,
            TelegramEnabled         = ns?.TelegramEnabled   ?? true,
            EmailEnabled            = ns?.EmailEnabled      ?? true,
            NotifyOnPurchase        = ns?.NotifyOnPurchase  ?? true,
            NotifyOnModeration      = ns?.NotifyOnModeration ?? true,
            NotifyOnBalance         = ns?.NotifyOnBalance   ?? true,
            NotifyOnBroadcast       = ns?.NotifyOnBroadcast ?? true,
        };
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
        return _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    public Task<User?> GetByIdWithSettingsAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users
           .Include(u => u.NotificationSettings)
           .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public Task<User?> GetUserByShopIdAsync(Guid shopId, CancellationToken ct = default) =>
        _db.Users
           .Include(u => u.NotificationSettings)
           .FirstOrDefaultAsync(u => u.ShopId == shopId, ct);

    public async Task<List<(long? TelegramId, string? Email, bool NotifyOnPurchase, bool NotifyOnModeration)>>
        GetUsersWithContactInfoAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(u => u.NotificationSettings)
            .Where(u => u.TelegramId != null || u.EmailAddress != null)
            .Select(u => new ValueTuple<long?, string?, bool, bool>(
                u.TelegramId,
                u.EmailAddress,
                u.NotificationSettings != null ? u.NotificationSettings.NotifyOnPurchase   : true,
                u.NotificationSettings != null ? u.NotificationSettings.NotifyOnModeration : true))
            .ToListAsync(ct);
    }
}
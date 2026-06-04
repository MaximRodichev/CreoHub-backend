using CreoHub.Application.DTO.ShopFollowDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ShopFollowRepository : IShopFollowRepository
{
    private readonly AppDbContext _db;

    public ShopFollowRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ShopFollow follow, CancellationToken ct = default)
    {
        await _db.ShopFollows.AddAsync(follow, ct);
    }

    public Task RemoveAsync(Guid userId, Guid shopId, CancellationToken ct = default) =>
        _db.ShopFollows
            .Where(f => f.UserId == userId && f.ShopId == shopId)
            .ExecuteDeleteAsync(ct);

    public Task<bool> IsFollowingAsync(Guid userId, Guid shopId, CancellationToken ct = default) =>
        _db.ShopFollows.AnyAsync(f => f.UserId == userId && f.ShopId == shopId, ct);

    public Task<int> CountFollowersAsync(Guid shopId, CancellationToken ct = default) =>
        _db.ShopFollows.CountAsync(f => f.ShopId == shopId, ct);

    public async Task<List<FollowerNotificationDto>> GetFollowersForNotificationAsync(
        Guid shopId, CancellationToken ct = default)
    {
        return await (
            from f in _db.ShopFollows.AsNoTracking()
            join u  in _db.Users on f.UserId equals u.Id
            join ns in _db.UserNotificationSettings on u.Id equals ns.UserId
            where f.ShopId == shopId && ns.NotifyOnNewProduct
            select new FollowerNotificationDto
            {
                UserId          = u.Id,
                TelegramId      = u.TelegramId,
                EmailAddress    = u.EmailAddress,
                TelegramEnabled = ns.TelegramEnabled,
                EmailEnabled    = ns.EmailEnabled,
            })
            .ToListAsync(ct);
    }
}

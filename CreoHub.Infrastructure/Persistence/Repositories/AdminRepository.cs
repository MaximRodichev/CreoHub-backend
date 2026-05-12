using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _db;

    public AdminRepository(AppDbContext db) => _db = db;

    // ── Пользователи ──────────────────────────────────────────────────────

    public async Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        // OrderByDescending must come BEFORE Select — EF Core can't sort by a DTO property
        return await _db.Users
            .OrderByDescending(u => u.RegistrationDate)
            .Select(u => new AdminUserDto(
                u.Id,
                u.Name,
                u.EmailAddress,
                u.Role.ToString(),
                u.LifetimeSpent,
                u.Discount,
                u.Orders.Count(o => o.Status == OrderStatus.Completed),
                u.RegistrationDate
            ))
            .ToListAsync(ct);
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return null;

        var orders = user.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(50)
            .Select(o => new AdminOrderSummaryDto(
                o.Id,
                o.Price,
                o.Status.ToString(),
                o.OrderDate,
                o.Items.Count
            ))
            .ToList();

        return new AdminUserDetailDto(
            user.Id,
            user.Name,
            user.EmailAddress,
            user.TelegramId,
            user.Role.ToString(),
            user.LifetimeSpent,
            user.Discount,
            user.RegistrationDate,
            orders
        );
    }

    // ── Продукты ──────────────────────────────────────────────────────────

    public async Task<List<AdminProductNameDto>> GetAllProductNamesAsync(CancellationToken ct = default)
    {
        return await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new AdminProductNameDto(p.Id, p.Name))
            .ToListAsync(ct);
    }

    // ── Магазины ──────────────────────────────────────────────────────────

    public async Task<List<AdminShopDto>> GetAllShopsAsync(CancellationToken ct = default)
    {
        // Выручка по каждому магазину — сумма ShopTransaction.FullAmount (включая платформенную комиссию)
        var revenues = await _db.ShopTransactions
            .Where(t => t.TransactionStatus == TransactionStatus.Completed)
            .GroupBy(t => t.ShopId)
            .Select(g => new { ShopId = g.Key, Total = g.Sum(t => t.FullAmount) })
            .ToDictionaryAsync(x => x.ShopId, x => x.Total, ct);

        // Materialize raw data first — revenues.GetValueOrDefault can't be translated to SQL
        var raw = await _db.Shops
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                OwnerName    = s.Owner.Name,
                s.OwnerId,
                ProductCount = s.Products.Count,
                s.CreatedAt,
            })
            .ToListAsync(ct);

        return raw
            .Select(s => new AdminShopDto(
                s.Id,
                s.Name,
                s.Description,
                s.OwnerName,
                s.OwnerId,
                s.ProductCount,
                revenues.GetValueOrDefault(s.Id, 0m),
                s.CreatedAt
            ))
            .ToList();
    }
}

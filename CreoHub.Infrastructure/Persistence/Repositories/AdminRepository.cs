using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
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
        // OrderByDescending must come BEFORE Select — EF Core can't sort by a DTO property.
        // Скидку не храним — считаем из LifetimeSpent в памяти (единый источник тиров).
        var rows = await _db.Users
            .OrderByDescending(u => u.RegistrationDate)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.EmailAddress,
                Role = u.Role.ToString(),
                u.LifetimeSpent,
                Completed = u.Orders.Count(o => o.Status == OrderStatus.Completed),
                u.RegistrationDate
            })
            .ToListAsync(ct);

        return rows.Select(r => new AdminUserDto(
            r.Id,
            r.Name,
            r.EmailAddress,
            r.Role,
            r.LifetimeSpent,
            User.LifetimeDiscountFor(r.LifetimeSpent) * 100m,
            r.Completed,
            r.RegistrationDate
        )).ToList();
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
            user.GetLifetimeDiscount() * 100m,
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

    // ── Дашборд активности ──────────────────────────────────────────────────

    public async Task<int> GetRegisteredCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Users.CountAsync(u => u.RegistrationDate >= from && u.RegistrationDate <= to, ct);

    public async Task<List<DailyRegistration>> GetDailyRegistrationsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.Users
            .Where(u => u.RegistrationDate >= from && u.RegistrationDate <= to)
            .GroupBy(u => u.RegistrationDate.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => new DailyRegistration(r.Day, r.Count)).ToList();
    }

    public async Task<Dictionary<Guid, string>> GetUserNamesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new();
        return await _db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }

    public async Task<Dictionary<int, string>> GetProductNamesAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new();
        return await _db.Products
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
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

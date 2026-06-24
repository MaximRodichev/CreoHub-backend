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

    // ── Объединение аккаунтов ───────────────────────────────────────────────

    public async Task<MergeUserSummaryDto?> GetMergeUserAsync(Guid id, CancellationToken ct = default)
    {
        var u = await _db.Users
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.Name, x.EmailAddress, x.TelegramId, x.TelegramUsername,
                HasShop = x.ShopId != null, Role = x.Role.ToString(), x.LifetimeSpent
            })
            .FirstOrDefaultAsync(ct);
        if (u is null) return null;

        var bal = await _db.UserBalances
            .Where(b => b.UserId == id)
            .Select(b => new { b.AvailableAmount, b.PendingAmount })
            .FirstOrDefaultAsync(ct);

        return new MergeUserSummaryDto(
            u.Id, u.Name, u.EmailAddress, u.TelegramId, u.TelegramUsername,
            u.HasShop, u.Role, u.LifetimeSpent,
            bal?.AvailableAmount ?? 0m, bal?.PendingAmount ?? 0m);
    }

    public async Task<MergeCountsDto> GetMergeCountsAsync(Guid m, CancellationToken ct = default)
    {
        return new MergeCountsDto(
            await _db.ContentAccesses.CountAsync(x => x.UserId == m, ct),
            await _db.Orders.CountAsync(x => x.CustomerId == m, ct),
            await _db.UserTransactions.CountAsync(x => x.UserId == m, ct),
            await _db.Subscriptions.CountAsync(x => x.UserId == m, ct),
            await _db.ShopFollows.CountAsync(x => x.UserId == m, ct),
            await _db.InAppNotifications.CountAsync(x => x.UserId == m, ct));
    }

    public async Task MergeAccountsAsync(Guid keepId, Guid mergeId, Guid adminId, CancellationToken ct = default)
    {
        var keep  = await GetMergeUserAsync(keepId, ct)  ?? throw new InvalidOperationException("Остающийся аккаунт не найден.");
        var merge = await GetMergeUserAsync(mergeId, ct) ?? throw new InvalidOperationException("Удаляемый аккаунт не найден.");

        // Защитные ре-проверки (на случай обхода command-уровня)
        if (keepId == mergeId)             throw new InvalidOperationException("Нельзя объединить аккаунт сам с собой.");
        if (keep.HasShop || merge.HasShop) throw new InvalidOperationException("Один из аккаунтов владеет магазином — мердж запрещён.");

        var counts = await GetMergeCountsAsync(mergeId, ct);

        // COALESCE: целевой сохраняет свои поля, иначе берёт у удаляемого (гарды исключают коллизию)
        object tg     = (object?)(keep.TelegramId       ?? merge.TelegramId)       ?? DBNull.Value;
        object tgUser = (object?)(keep.TelegramUsername  ?? merge.TelegramUsername) ?? DBNull.Value;
        object email  = (object?)(keep.Email             ?? merge.Email)            ?? DBNull.Value;
        var spent     = merge.LifetimeSpent;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) Купленные файлы — дубли убрать, остальное перенести
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"ContentAccesses\" s WHERE s.\"UserId\"={1} AND EXISTS (SELECT 1 FROM \"ContentAccesses\" t WHERE t.\"UserId\"={0} AND t.\"ContentFileId\"=s.\"ContentFileId\")",
            new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"ContentAccesses\" SET \"UserId\"={0} WHERE \"UserId\"={1}", new object[] { keepId, mergeId }, ct);

        // 2-4) Заказы / транзакции / подписки
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"Orders\" SET \"CustomerId\"={0} WHERE \"CustomerId\"={1}", new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"UserTransactions\" SET \"UserId\"={0} WHERE \"UserId\"={1}", new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"Subscriptions\" SET \"UserId\"={0} WHERE \"UserId\"={1}", new object[] { keepId, mergeId }, ct);

        // 5) Подписки на магазины — дубли убрать, остальное перенести
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"ShopFollows\" s WHERE s.\"UserId\"={1} AND EXISTS (SELECT 1 FROM \"ShopFollows\" t WHERE t.\"UserId\"={0} AND t.\"ShopId\"=s.\"ShopId\")",
            new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"ShopFollows\" SET \"UserId\"={0} WHERE \"UserId\"={1}", new object[] { keepId, mergeId }, ct);

        // 6) Запросы продавцу / уведомления / промокоды / аналитика
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"ShopRequests\" SET \"BuyerUserId\"={0} WHERE \"BuyerUserId\"={1}", new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"InAppNotifications\" SET \"UserId\"={0} WHERE \"UserId\"={1}", new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"SubscriptionPromoCodes\" SET \"IssuedToUserId\"={0} WHERE \"IssuedToUserId\"={1}", new object[] { keepId, mergeId }, ct);
        await _db.Database.ExecuteSqlRawAsync("UPDATE \"UserEvents\" SET \"UserId\"={0} WHERE \"UserId\"={1}", new object[] { keepId, mergeId }, ct);

        // 7) Баланс merge → keep
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"UserBalances\" t SET \"AvailableAmount\"=t.\"AvailableAmount\"+s.\"AvailableAmount\", \"PendingAmount\"=t.\"PendingAmount\"+s.\"PendingAmount\" FROM \"UserBalances\" s WHERE t.\"UserId\"={0} AND s.\"UserId\"={1}",
            new object[] { keepId, mergeId }, ct);

        // 8) Удаляем merge-юзера → каскад Cart(+items+files) и NotificationSettings
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"Users\" WHERE \"Id\"={0}", new object[] { mergeId }, ct);

        // 9) Осиротевший баланс merge (UserBalances.UserId — не FK, каскада нет)
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM \"UserBalances\" WHERE \"UserId\"={0}", new object[] { mergeId }, ct);

        // 10) Телега + почта + спенд на keep (merge удалён → уникальность свободна)
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Users\" SET \"TelegramId\"={1}, \"TelegramUsername\"={2}, \"EmailAddress\"={3}, \"LifetimeSpent\"=\"LifetimeSpent\"+{4} WHERE \"Id\"={0}",
            new object[] { keepId, tg, tgUser, email, spent }, ct);

        // Аудит — в той же транзакции
        _db.AccountMergeLogs.Add(new AccountMergeLog(
            keepId, mergeId, merge.Name, merge.Email, merge.TelegramId, merge.TelegramUsername, adminId,
            counts.ContentAccess, counts.Orders, counts.Transactions, counts.Subscriptions,
            merge.Balance, spent));
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }
}

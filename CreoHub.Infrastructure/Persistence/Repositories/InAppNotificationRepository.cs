using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class InAppNotificationRepository : IInAppNotificationRepository
{
    private readonly AppDbContext _db;

    public InAppNotificationRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Guid userId, NotificationType type, string message,
                               string? actionUrl = null, CancellationToken ct = default)
    {
        var n = InAppNotification.Create(userId, type, message, actionUrl);
        await _db.InAppNotifications.AddAsync(n, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<InAppNotification>> GetPendingAsync(Guid userId, int limit = 20,
                                                         CancellationToken ct = default) =>
        _db.InAppNotifications
           .Where(n => n.UserId == userId && !n.IsRead)
           .OrderByDescending(n => n.CreatedAt)
           .Take(limit)
           .ToListAsync(ct);

    public Task<List<InAppNotification>> GetHistoryAsync(Guid userId, NotificationType? type,
                                                         int page, int pageSize,
                                                         CancellationToken ct = default)
    {
        var query = _db.InAppNotifications.AsNoTracking().Where(n => n.UserId == userId);
        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        return query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        _db.InAppNotifications
           .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<bool> AcknowledgeAsync(int id, Guid userId, CancellationToken ct = default)
    {
        var n = await _db.InAppNotifications
                         .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (n is null) return false;
        n.Acknowledge();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AcknowledgeAllAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _db.InAppNotifications
                             .Where(n => n.UserId == userId && !n.IsRead)
                             .ToListAsync(ct);
        foreach (var n in items) n.Acknowledge();
        if (items.Count > 0)
            await _db.SaveChangesAsync(ct);
    }
}

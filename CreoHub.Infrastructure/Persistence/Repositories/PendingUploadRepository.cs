using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class PendingUploadRepository : IPendingUploadRepository
{
    private readonly AppDbContext _db;

    public PendingUploadRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PendingUpload pending, CancellationToken ct = default)
    {
        await _db.PendingUploads.AddAsync(pending, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PendingUpload?> ConsumeAsync(string key, Guid shopId, CancellationToken ct = default)
    {
        var record = await _db.PendingUploads
            .FirstOrDefaultAsync(p => p.Key == key && p.ShopId == shopId, ct);

        if (record is null || record.IsExpired)
        {
            if (record is not null)
                _db.PendingUploads.Remove(record); // чистим просроченную
            await _db.SaveChangesAsync(ct);
            return null;
        }

        _db.PendingUploads.Remove(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.PendingUploads
            .Where(p => p.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);
    }
}

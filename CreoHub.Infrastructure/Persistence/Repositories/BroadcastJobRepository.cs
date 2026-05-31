using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class BroadcastJobRepository : IBroadcastJobRepository
{
    private readonly AppDbContext _db;

    public BroadcastJobRepository(AppDbContext db) => _db = db;

    public async Task<BroadcastJob> AddAsync(BroadcastJob job, CancellationToken ct = default)
    {
        var entry = await _db.BroadcastJobs.AddAsync(job, ct);
        return entry.Entity;
    }

    public Task<BroadcastJob?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.BroadcastJobs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<BroadcastJob>> GetAllAsync(CancellationToken ct = default) =>
        _db.BroadcastJobs.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public Task<List<BroadcastJob>> GetPendingAsync(CancellationToken ct = default) =>
        _db.BroadcastJobs
            .Where(x => x.Status == BroadcastStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task ResetStuckRunningJobsAsync(CancellationToken ct = default)
    {
        var stuck = await _db.BroadcastJobs
            .Where(x => x.Status == BroadcastStatus.Running)
            .ToListAsync(ct);

        foreach (var job in stuck)
            job.Fail("Interrupted by unexpected server restart");

        if (stuck.Count > 0)
            _db.BroadcastJobs.UpdateRange(stuck);
    }

    public BroadcastJob Update(BroadcastJob job) =>
        _db.BroadcastJobs.Update(job).Entity;
}

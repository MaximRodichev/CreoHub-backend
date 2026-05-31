using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IBroadcastJobRepository
{
    Task<BroadcastJob>         AddAsync(BroadcastJob job, CancellationToken ct = default);
    Task<BroadcastJob?>        GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<BroadcastJob>>   GetAllAsync(CancellationToken ct = default);
    Task<List<BroadcastJob>>   GetPendingAsync(CancellationToken ct = default);
    Task                       ResetStuckRunningJobsAsync(CancellationToken ct = default);
    BroadcastJob               Update(BroadcastJob job);
}

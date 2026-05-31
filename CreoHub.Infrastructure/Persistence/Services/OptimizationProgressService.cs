using System.Collections.Concurrent;
using CreoHub.Application.Services;

namespace CreoHub.Infrastructure.Persistence.Services;

public class OptimizationProgressService : IOptimizationProgressService
{
    private readonly ConcurrentDictionary<Guid, int> _progress = new();

    public void SetProgress(Guid id, int percent) => _progress[id] = percent;
    public int  GetProgress(Guid id) => _progress.TryGetValue(id, out var p) ? p : 0;
    public void Remove(Guid id)      => _progress.TryRemove(id, out _);
}

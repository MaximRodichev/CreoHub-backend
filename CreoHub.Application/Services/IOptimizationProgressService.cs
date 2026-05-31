namespace CreoHub.Application.Services;

/// <summary>
/// In-memory хранилище прогресса FFmpeg-оптимизации.
/// Singleton — живёт всё время жизни процесса.
/// Значения: 0-95 = FFmpeg converts, 96 = thumbnail, 100 = done, -1 = failed.
/// </summary>
public interface IOptimizationProgressService
{
    void SetProgress(Guid id, int percent);
    int  GetProgress(Guid id);   // 0 если не найден / не начат
    void Remove(Guid id);
}

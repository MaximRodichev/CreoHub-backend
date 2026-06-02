using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;

namespace CreoHub.Application.Repositories;

public interface IInAppNotificationRepository
{
    Task AddAsync(Guid userId, NotificationType type, string message,
                  string? actionUrl = null, CancellationToken ct = default);

    Task<List<InAppNotification>> GetPendingAsync(Guid userId, int limit = 20,
                                                  CancellationToken ct = default);

    /// <summary>
    /// Полная история уведомлений (прочитанные + непрочитанные), новейшие сверху.
    /// Опциональный фильтр по типу + пагинация.
    /// </summary>
    Task<List<InAppNotification>> GetHistoryAsync(Guid userId, NotificationType? type,
                                                  int page, int pageSize,
                                                  CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<bool> AcknowledgeAsync(int id, Guid userId, CancellationToken ct = default);

    Task AcknowledgeAllAsync(Guid userId, CancellationToken ct = default);
}

using CreoHub.Application.DTO.ShopFollowDTOs;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Repositories;

public interface IShopFollowRepository
{
    /// <summary>Добавляет подписку в контекст (SaveChanges — через IUnitOfWork в хендлере).</summary>
    Task AddAsync(ShopFollow follow, CancellationToken ct = default);

    /// <summary>Удаляет подписку немедленно (ExecuteDelete). SaveChanges не требуется.</summary>
    Task RemoveAsync(Guid userId, Guid shopId, CancellationToken ct = default);

    Task<bool> IsFollowingAsync(Guid userId, Guid shopId, CancellationToken ct = default);

    Task<int> CountFollowersAsync(Guid shopId, CancellationToken ct = default);

    /// <summary>
    /// Подписчики магазина, у которых включено уведомление о новых товарах.
    /// Один запрос с JOIN на Users + NotificationSettings.
    /// </summary>
    Task<List<FollowerNotificationDto>> GetFollowersForNotificationAsync(
        Guid shopId, CancellationToken ct = default);
}

using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IAccountRepository : IRepository<User, Guid>
{
    public Task<Guid> GetShopByUserId(Guid userId);
    public Task<UserProfileDTO> GetUserProfileByUserId(Guid userId);
    public Task<User?> FindUserByCredentials(string? email, long? telegramId);
    public Task<List<ClientShortInfoDTO>> GetClientsShortInfoAsync(Guid shopId);
    public Task<User?> GetFullInfoByIdAsync(Guid userId);

    /// <summary>Find the user who owns the given shop. Includes NotificationSettings.</summary>
    public Task<User?> GetUserByShopIdAsync(Guid shopId, CancellationToken ct = default);

    /// <summary>Load user with their NotificationSettings (for updating preferences).</summary>
    public Task<User?> GetByIdWithSettingsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Get all users who have a Telegram or email address (for broadcasts).</summary>
    public Task<List<(long? TelegramId, string? Email, bool NotifyOnPurchase, bool NotifyOnModeration)>>
        GetUsersWithContactInfoAsync(CancellationToken ct = default);
}
using CreoHub.Application.DTO.AdminDTOs;

namespace CreoHub.Application.Repositories;

public interface IAdminRepository
{
    Task<List<AdminUserDto>>        GetAllUsersAsync(CancellationToken ct = default);
    Task<AdminUserDetailDto?>       GetUserDetailAsync(Guid userId, CancellationToken ct = default);
    Task<List<AdminShopDto>>        GetAllShopsAsync(CancellationToken ct = default);
    Task<List<AdminProductNameDto>> GetAllProductNamesAsync(CancellationToken ct = default);

    // ── Activity dashboard ──────────────────────────────────────────────────
    Task<int> GetRegisteredCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<DailyRegistration>> GetDailyRegistrationsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<Dictionary<int, string>>  GetProductNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

    // ── Объединение аккаунтов ───────────────────────────────────────────────
    Task<MergeUserSummaryDto?> GetMergeUserAsync(Guid id, CancellationToken ct = default);
    Task<MergeCountsDto>       GetMergeCountsAsync(Guid mergeUserId, CancellationToken ct = default);
    /// <summary>Переносит данные mergeUser → keepUser, удаляет mergeUser, пишет аудит. Всё в одной транзакции.</summary>
    Task MergeAccountsAsync(Guid keepUserId, Guid mergeUserId, Guid adminId, CancellationToken ct = default);
}

public record DailyRegistration(DateTime Day, int Count);

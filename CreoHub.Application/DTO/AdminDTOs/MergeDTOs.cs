namespace CreoHub.Application.DTO.AdminDTOs;

/// <summary>Краткая сводка по аккаунту для предпросмотра мерджа + проверки гардов.</summary>
public record MergeUserSummaryDto(
    Guid    Id,
    string  Name,
    string? Email,
    long?   TelegramId,
    string? TelegramUsername,
    bool    HasShop,
    string  Role,
    decimal LifetimeSpent,
    decimal Balance,
    decimal PendingBalance);

/// <summary>Что переедет с удаляемого аккаунта на остающийся (счётчики).</summary>
public record MergeCountsDto(
    int ContentAccess,
    int Orders,
    int Transactions,
    int Subscriptions,
    int ShopFollows,
    int Notifications);

/// <summary>Предпросмотр мерджа: оба аккаунта + что переедет + вердикт гардов.</summary>
public record MergePreviewDto(
    MergeUserSummaryDto  Keep,
    MergeUserSummaryDto  Merge,
    MergeCountsDto       Moves,
    bool                 CanMerge,
    List<string>         Blockers);

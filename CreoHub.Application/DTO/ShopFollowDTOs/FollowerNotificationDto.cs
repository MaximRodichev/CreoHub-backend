namespace CreoHub.Application.DTO.ShopFollowDTOs;

/// <summary>
/// Проекция подписчика для рассылки уведомлений о новом товаре.
/// Собирается одним запросом (JOIN Users + NotificationSettings), без N+1.
/// </summary>
public class FollowerNotificationDto
{
    public Guid    UserId          { get; init; }
    public long?   TelegramId      { get; init; }
    public string? EmailAddress    { get; init; }
    public bool    TelegramEnabled { get; init; }
    public bool    EmailEnabled    { get; init; }
}

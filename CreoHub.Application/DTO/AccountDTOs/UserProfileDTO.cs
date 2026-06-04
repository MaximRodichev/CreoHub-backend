using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.AccountDTOs;

public class UserProfileDTO
{
    public Guid    Id               { get; set; }
    public string  Name             { get; set; }
    public string? Email            { get; set; }
    public long?   TelegramId       { get; set; }
    public string? TelegramUsername { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string?  shopName         { get; set; }
    public Guid?    shopId           { get; set; }
    public string   Role             { get; set; }
    public decimal  LifetimeSpent            { get; set; }
    public decimal  LifetimeDiscountPercent  { get; set; }

    // ── Notification settings (from UserNotificationSettings) ─────────────────
    public bool TelegramEnabled    { get; set; } = true;
    public bool EmailEnabled       { get; set; } = true;
    public bool NotifyOnPurchase   { get; set; } = true;
    public bool NotifyOnModeration { get; set; } = true;
    public bool NotifyOnBalance    { get; set; } = true;
    public bool NotifyOnBroadcast  { get; set; } = true;
    public bool NotifyOnNewProduct { get; set; } = true;
}

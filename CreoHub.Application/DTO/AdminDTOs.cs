namespace CreoHub.Application.DTO.AdminDTOs;

// ── Входящие ──────────────────────────────────────────────────────────────
public record AdminCreateClientDto(
    string  Name,
    long?   TelegramId,
    string? TelegramUsername,
    string? Email
);

// ── Пользователи ──────────────────────────────────────────────────────────
public record AdminUserDto(
    Guid     Id,
    string   Name,
    string?  Email,
    string   Role,
    decimal  LifetimeSpent,
    decimal  Discount,
    int      OrderCount,
    DateTime RegistrationDate
);

public record AdminUserDetailDto(
    Guid                    Id,
    string                  Name,
    string?                 Email,
    long?                   TelegramId,
    string                  Role,
    decimal                 LifetimeSpent,
    decimal                 Discount,
    DateTime                RegistrationDate,
    List<AdminOrderSummaryDto> RecentOrders
);

public record AdminOrderSummaryDto(
    Guid     Id,
    decimal  Price,
    string   Status,
    DateTime OrderDate,
    int      ItemCount
);

// ── Продукты (для автокомплита) ───────────────────────────────────────────
public record AdminProductNameDto(int Id, string Name);

// ── Магазины ──────────────────────────────────────────────────────────────
public record AdminShopDto(
    Guid    Id,
    string  Name,
    string  Description,
    string  OwnerName,
    Guid    OwnerId,
    int     ProductCount,
    decimal TotalRevenue,
    DateTime CreatedAt
);

namespace CreoHub.Application.DTO.ShopDTOs;

public record ShopStatsDTO(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalProducts,
    int TotalClients,
    Dictionary<string, decimal> RevenuePerMonth,
    // Шаг временной оси графика: "day" | "week" | "month" (выбирается по длине периода).
    // Ключи RevenuePerMonth — ISO-дата начала бакета "yyyy-MM-dd".
    string Granularity = "month");
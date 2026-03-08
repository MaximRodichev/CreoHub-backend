namespace CreoHub.Application.DTO.ShopDTOs;

public record ShopStatsDTO(
    decimal TotalRevenue, 
    int TotalOrders, 
    int TotalProducts, 
    int TotalClients,
    Dictionary<string, decimal> RevenuePerMonth);
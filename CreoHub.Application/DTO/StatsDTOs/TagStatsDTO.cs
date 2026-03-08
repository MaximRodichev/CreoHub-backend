namespace CreoHub.Application.DTO.StatsDTOs;

public record TagStatsDTO
{
    public int TagId { get; init; }
    public string TagName { get; init; }
    public decimal TagRevenue { get; init; }
    public int TagProductsCount { get; init; }
    public int TagItemsSold { get; init; }
    public int TagOrdersCount { get; init; }
}
namespace CreoHub.Application.DTO.StatsDTOs;

public record ProductStatsDTO
{
    public Guid Id  { get; init; }
    public string Name { get; init; }
    public decimal Revenue { get; init; }
    public int OrdersCount { get; init; }
    public decimal Price { get; init; }
}
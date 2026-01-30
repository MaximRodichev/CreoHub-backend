namespace CreoHub.Application.DTO;

public record FiltersDto
{
    public Guid? ShopId { get; init; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<string>? Tags { get; set; }
    public string? Search { get; set; }
}
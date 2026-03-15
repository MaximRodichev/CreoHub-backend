namespace CreoHub.Application.DTO;

public record FiltersDto
{
    public Guid? ShopId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<string>? Tags { get; set; }
    public string? Search { get; set; }
    public SortOrder? SortOrder { get; set; }
}

public enum SortOrder
{
    None,
    AscendingPrice,
    DescendingPrice,
    Popularity,
    Latests,
    Oldest
}
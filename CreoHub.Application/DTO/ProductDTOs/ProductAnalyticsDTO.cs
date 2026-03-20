using System.Text.Json.Serialization;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.ProductDTOs;

public record ProductAnalyticsDTO
{
    public Guid ShopId { get; set; }
    public int ProductId { get; set; }
    public DateTime? LastSellDate { get; set; }
    public int CountSells { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductStatus ProductStatus { get; set; }
    public Dictionary<DateTime, decimal> PriceHistory { get; set; }
    public ICollection<DateTime> SellsDateTimes { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductType ProductType { get; set; }
    public List<ProductShortInfoDTO>? inBundleProducts { get; set; }
    public List<StorageObjectViewDTO> MediaViews { get; set; }
    
}
using System.Text.Json.Serialization;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.ProductDTOs;

public record ProductInfoDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    // от 
    public bool isHotProduct { get; set; } = false;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; }
    public List<string> Tags { get; set; }
    public DateTime Date { get; set; }
    public List<StorageObjectViewDTO> MediaViews { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductType ProductType { get; set; }
    public List<ProductShortInfoDTO>? inBundleProducts { get; set; }
}
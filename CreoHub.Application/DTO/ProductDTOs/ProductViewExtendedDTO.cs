using System.Text.Json.Serialization;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.ProductDTOs;

public class ProductViewExtendedDTO : ProductViewDTO
{
    public int SellsCount { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductStatus ProductStatus { get; set; }
}
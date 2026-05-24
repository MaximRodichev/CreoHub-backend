using System.Text.Json.Serialization;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.ProductDTOs;

public class ProductViewDTO
{
    public int Id { get; set; }
    public bool isHotProduct { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductType ProductType { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public decimal? PriceWithoutDiscount { get; set; }
    public decimal Price { get; set; }
    public string PreviewKey { get; set; }
    public string? PreviewThumbnailKey { get; set; }
}
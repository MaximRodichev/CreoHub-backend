namespace CreoHub.Application.DTO.ShopDTOs;

public class ShopPublicDTO
{
    public Guid     Id            { get; set; }
    public string   Name          { get; set; } = "";
    public string   Description   { get; set; } = "";
    public DateTime CreatedAt     { get; set; }
    public string?  BannerKey     { get; set; }
    public string?  LogoKey       { get; set; }
    public int      TotalProducts { get; set; }
    public int      TotalDeals    { get; set; }
    public List<ShopPublicProductDTO> Products { get; set; } = new();
}

public class ShopPublicProductDTO
{
    public int     Id                     { get; set; }
    public string  Name                   { get; set; } = "";
    public decimal Price                  { get; set; }
    public decimal PriceWithoutDiscount   { get; set; }
    public string? PreviewKey             { get; set; }
    public string? PreviewThumbnailKey    { get; set; }
    public string  ProductType            { get; set; } = "";
    public bool    IsHotProduct           { get; set; }
}

namespace CreoHub.Application.DTO.ShopRequestDTOs;

/// <summary>Что видит продавец в своём списке запросов.</summary>
public class ShopRequestDTO
{
    public int       Id          { get; set; }
    public string    BuyerName   { get; set; } = string.Empty;
    public string    Message     { get; set; } = string.Empty;
    public string    Status      { get; set; } = string.Empty;
    public string?   SellerReply { get; set; }
    public DateTime  CreatedAt   { get; set; }
    public DateTime? RepliedAt   { get; set; }
}

/// <summary>Что видит покупатель в своём списке «Мои предложения» (с названием магазина).</summary>
public class MyShopRequestDTO
{
    public int       Id          { get; set; }
    public Guid      ShopId      { get; set; }
    public string    ShopName    { get; set; } = string.Empty;
    public string    Message     { get; set; } = string.Empty;
    public string    Status      { get; set; } = string.Empty;
    public string?   SellerReply { get; set; }
    public DateTime  CreatedAt   { get; set; }
    public DateTime? RepliedAt   { get; set; }
}

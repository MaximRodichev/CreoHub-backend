namespace CreoHub.Application.DTO.ShopDTOs;

public class ClientShortInfoDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string TelegramUsername { get; set; }
    public decimal TotalSpent { get; set; }
    public int TotalBuys { get; set; }
}
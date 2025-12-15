namespace CreoHub.Application.DTO.ShopDTOs;

public class ShopShortInfoDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; }
    public int CountProducts { get; set; }
}
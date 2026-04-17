using CreoHub.Application.DTO.StorageDTOs;

namespace CreoHub.Application.DTO.CartDTOs;

public class CartItemDTO
{
    public Guid CartItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? PreviewKey { get; set; }
    public string? PreviewThumbnailKey { get; set; }
    public List<Guid> SelectedContentItems { get; set; } = new();
    public List<ContentFileInfo> ContentFileInfos { get; set; } = new();
}
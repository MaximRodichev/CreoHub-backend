namespace CreoHub.Domain.Entities;

public class CartItemFile
{
    public Guid CartItemId { get; set; }
    public Guid ContentFileId { get; set; }
    public CartItem CartItem { get; set; }
    public ContentFile ContentFile { get; set; }
}
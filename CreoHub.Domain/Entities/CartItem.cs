namespace CreoHub.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public int ProductId { get; private set; }
    public DateTime AddedAt { get; private set; }
    public ICollection<CartItemFile> SelectedFiles { get; private set; } = new List<CartItemFile>();

    public Cart Cart { get; private set; }
    public Product Product { get; private set; }

    public static CartItem Create(Guid cartId, int productId, IEnumerable<Guid> fileIds)
    {
        var item = new CartItem { Id = Guid.NewGuid(), CartId = cartId, ProductId = productId, AddedAt = DateTime.UtcNow };
        foreach (var fileId in fileIds)
            item.SelectedFiles.Add(new CartItemFile { CartItemId = item.Id, ContentFileId = fileId });
        return item;
    }

    public void UpdateFiles(IEnumerable<Guid> fileIds)
    {
        SelectedFiles.Clear();
        foreach (var id in fileIds)
            SelectedFiles.Add(new CartItemFile { CartItemId = Id, ContentFileId = id });
    }
}
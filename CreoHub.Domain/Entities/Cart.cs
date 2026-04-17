namespace CreoHub.Domain.Entities;

public class Cart
{
    public Guid Id { get; private set; }
    public User User { get; private set; }
    public Guid UserId { get; private set; }
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    private const int MaxItems = 99;

    public static Cart Create(Guid userId) => new Cart { Id = Guid.NewGuid(), UserId = userId };

    public CartItem AddItem(int productId, IEnumerable<Guid> fileIds)
    {
        if (Items.Count >= MaxItems) throw new InvalidOperationException("Cart is full (max 99 items)");
        var item = CartItem.Create(Id, productId, fileIds);
        Items.Add(item);
        return item;
    }

    public void RemoveItem(Guid itemId) => Items.Remove(Items.First(i => i.Id == itemId));
    public void Clear() => Items.Clear();
}
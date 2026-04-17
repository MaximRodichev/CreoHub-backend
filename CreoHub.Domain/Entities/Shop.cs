namespace CreoHub.Domain.Entities;

public class Shop
{
    private readonly List<Product> _products = new();
    private readonly List<StorageObject> _files = new();

    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();
    public IReadOnlyCollection<StorageObject> Files => _files.AsReadOnly();
    public User Owner { get; private init; }
    public Guid OwnerId { get; private init; }
    public IReadOnlyCollection<ShopTransaction> Transactions { get; private set; } = new List<ShopTransaction>();
    public ShopBalance Balance { get; private init; }
    public Guid BalanceId { get; private init; }

    private Shop() {}

    public Shop(string name, string description, Guid ownerId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        OwnerId = ownerId;
        Balance = new ShopBalance(Id);
    }
    
    public void UpdateName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
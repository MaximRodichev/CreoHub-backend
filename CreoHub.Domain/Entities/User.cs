using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;


public class User
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Name { get; private set; }
    public decimal Discount { get; private set; } = 0;
    public long? TelegramId { get; private init; }
    public string? TelegramUsername { get; private init; }
    public string? EmailAddress { get; private set; }
    public DateTime RegistrationDate { get; private init; } = DateTime.UtcNow;

    public UserRole Role { get; private set; } = UserRole.User;

    // FK
    public IReadOnlyCollection<Order> Orders { get; private set; } = new List<Order>();
    public Shop? Shop { get; private set; }
    public Guid? ShopId { get; private set; }

    public IReadOnlyCollection<UserTransaction> Transactions { get; private set; } = new List<UserTransaction>();
    public UserBalance Balance { get; private set; }
    public Guid BalanceId { get; private set; }
    
    public Cart Cart { get; private set; }
    public Guid CartId { get; private set; }
    
    public IReadOnlyCollection<ContentAccess> ContentAccesses { get; private set; } = new List<ContentAccess>();

    private User() {}

    public static User Create(string name, string? email = null,
        long? telegramId = null, string? telegramUsername = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        var user = new User
        {
            Name = name,
            EmailAddress = email,
            TelegramId = telegramId,
            TelegramUsername = telegramUsername,
        };
        user.Cart = Entities.Cart.Create(user.Id);
        user.CartId = user.Cart.Id;
        user.Balance = new UserBalance(user.Id);
        user.BalanceId = user.Balance.Id;

        return user;
    }

    public void AssignShop(Shop shop)
    {
        if (shop == null)
            throw new ArgumentNullException(nameof(shop));
        if (Shop != null)
            throw new InvalidOperationException("User already has a shop.");

        Shop = shop;
        ShopId = shop.Id;
        ChangeRole(UserRole.Shop);
    }

    public void ChangeRole(UserRole role)
    {
        if (Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot change admin role.");
        Role = role;
    }

    public void RecalculateDiscount(decimal totalPurchases)
    {
        if (totalPurchases < 0)
            throw new ArgumentException("Total purchases cannot be negative.");

        Discount = totalPurchases switch
        {
            >= 1000 => 7,
            >= 500 => 5,
            >= 300 => 3,
            _ => 0
        };
    }
}

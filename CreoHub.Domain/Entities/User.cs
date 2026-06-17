using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;


public class User
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Name { get; private set; }

    /// <summary>Накопленная сумма всех покупок пользователя (то, что он реально заплатил).</summary>
    public decimal LifetimeSpent { get; private set; } = 0m;
    public long? TelegramId { get; private set; }
    public string? TelegramUsername { get; private set; }
    public string? EmailAddress { get; private set; }
    public DateTime RegistrationDate { get; private init; } = DateTime.UtcNow;

    public UserRole Role { get; private set; } = UserRole.User;

    // ── Notification settings (1:1) ──────────────────────────────────────────
    public UserNotificationSettings? NotificationSettings { get; private set; }

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
        user.NotificationSettings = UserNotificationSettings.CreateDefault(user.Id);

        return user;
    }

    /// <summary>
    /// Привязывает Telegram к существующему аккаунту (Google-пользователь добавляет Telegram).
    /// </summary>
    public void LinkTelegram(long telegramId, string? username)
    {
        if (TelegramId.HasValue)
            throw new InvalidOperationException("Telegram уже привязан к этому аккаунту.");
        TelegramId = telegramId;
        TelegramUsername = username;
    }

    /// <summary>
    /// Привязывает Email к существующему аккаунту (Telegram-пользователь добавляет Google/Email).
    /// </summary>
    public void LinkEmail(string email)
    {
        if (!string.IsNullOrWhiteSpace(EmailAddress))
            throw new InvalidOperationException("Email уже привязан к этому аккаунту.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        EmailAddress = email;
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

    /// <summary>
    /// Увеличивает накопленный спенд после успешной покупки.
    /// </summary>
    public void AddSpend(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        LifetimeSpent += amount;
    }

    /// <summary>
    /// Скидка по накопленному спенду (F1) — ЕДИНСТВЕННЫЙ источник тиров.
    /// Доля [0, 0.09]. Результат жёстко ограничен формулой: что ни впиши в spent
    /// (хоть подделай БД) — выше 9% не станет. Скидка нигде не хранится, только считается.
    /// </summary>
    public static decimal LifetimeDiscountFor(decimal spent) => spent switch
    {
        >= 5000m => 0.09m,
        >= 2500m => 0.07m,
        >= 1000m => 0.05m,
        >= 500m  => 0.03m,
        >= 250m  => 0.02m,
        _        => 0m,
    };

    /// <summary>Скидка текущего пользователя по его LifetimeSpent.</summary>
    public decimal GetLifetimeDiscount() => LifetimeDiscountFor(LifetimeSpent);

    /// <summary>Скидка лид-магнита на первый заказ (доля). Применяется один раз — пока нет ни одной покупки.</summary>
    public const decimal FirstOrderDiscountRate = 0.20m;

    /// <summary>true пока пользователь ничего не покупал (LifetimeSpent растёт только после оплаченного заказа).</summary>
    public bool IsFirstOrder => LifetimeSpent == 0m;

    /// <summary>Скидка новому покупателю на первый заказ (−20%), иначе 0.</summary>
    public decimal GetWelcomeDiscount() => IsFirstOrder ? FirstOrderDiscountRate : 0m;

    /// <summary>
    /// Итоговая персональная скидка покупателя: лучшая из lifetime и welcome.
    /// На первом заказе lifetime = 0, поэтому возвращается welcome (−20%);
    /// на последующих welcome = 0, остаётся lifetime. Они взаимоисключающие.
    /// </summary>
    public decimal GetPersonalDiscount() => Math.Max(GetLifetimeDiscount(), GetWelcomeDiscount());

    /// <summary>
    /// Смена отображаемого имени пользователем (профиль). Имена НЕ уникальны.
    /// </summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя не может быть пустым.", nameof(name));
        name = name.Trim();
        if (name.Length > 50)
            throw new ArgumentException("Имя не должно превышать 50 символов.", nameof(name));
        Name = name;
    }
}

using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public double Discount { get; set; } = 0;
    public long? TelegramId { get; set; }
    public string? TelegramUsername { get; set; }
    public string? EmailAddress { get; set; }
    public DateTime RegistrationDate { get; set; } =  DateTime.Now;

    public UserRole Role { get; set; } = UserRole.User;
    
    // FK
    public ICollection<Order> Orders { get; set; }
    public Shop Shop { get; set; }
    public Guid? ShopId { get; set; }
    
    public User()
    {
        
    }

    public static User Create(string name, string? email = null, long? telegramId = null, string TelegramUsername = null)
    {
        return new User
        {
            Name = name,
            EmailAddress = email,
            TelegramId = telegramId,
            TelegramUsername = TelegramUsername,
        };
    }

    public User ChangeRole(UserRole role)
    {
        if (Role == UserRole.Admin)
        {
            return this;
        }
        Role = role;
        return this;
    }

    public User InjectDate(DateTime date)
    {
        RegistrationDate = date;
        return this;
    }

}
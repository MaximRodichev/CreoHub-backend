namespace CreoHub.Application.DTO.AccountDTOs;

public class IdentityDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? EmailAddress { get; set; }
    public long? TelegramId { get; set; }
    public Guid? ShopId { get; set; }

    public override string ToString()
    {
        return $"{Id} => {Name}";
    }
}
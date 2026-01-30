using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.AccountDTOs;

public class UserProfileDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public long? TelegramId { get; set; }
    public string? TelegramUsername { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string shopName  { get; set; }
    public Guid? shopId { get; set; }
    public string Role { get; set; }
}
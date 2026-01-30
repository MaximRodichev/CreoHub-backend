using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Domain.Entities;

namespace CreoHub.API.Models;

public class UserClaimsModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? EmailAddress { get; set; }
    public long? TelegramId { get; set; }
    public Guid? ShopId { get; set; }

    public UserClaimsModel(IdentityDTO user)
    {
        this.Id =  user.Id;
        this.Name = user.Name;
        this.EmailAddress = user.EmailAddress;
        this.TelegramId = user.TelegramId;
        this.ShopId = user.ShopId;
    }
}
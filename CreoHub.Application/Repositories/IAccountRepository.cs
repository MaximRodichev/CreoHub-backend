using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IAccountRepository : IRepository<User, Guid>
{
    public Task<Guid> GetShopByUserId(Guid userId);
    public Task<UserProfileDTO> GetUserProfileByUserId(Guid userId);
    public Task<User?> FindUserByCredentials(string? email, long? telegramId);
    public Task<List<ClientShortInfoDTO>> GetClientsShortInfoAsync(Guid shopId);
    public Task<User?> GetFullInfoByIdAsync(Guid userId);
}
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IAccountRepository : IRepository<User, Guid>
{
    public Task<Guid> GetShopByUserId(Guid userId);
    public Task<User?> FindUserByCredentials(string? email, long? telegramId);
    public Task<User?> GetFullInfoByIdAsync(Guid userId);
}
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IAccountRepository : IRepository<User, Guid>
{
    public Task<Guid> GetShopByUserId(Guid userId);
}
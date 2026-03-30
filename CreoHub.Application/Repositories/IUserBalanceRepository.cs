using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IUserBalanceRepository : IRepository<UserBalance, Guid>
{
    Task<UserBalance?> GetByUserIdAsync(Guid userId);
}

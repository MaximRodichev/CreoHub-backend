using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IBalanceRepository : IRepository<Balance, Guid>
{
    Task<Balance?> GetByOwnerIdAsync(Guid ownerId);
}

using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IUserTransactionRepository : IRepository<UserTransaction, Guid>
{
    Task<UserTransaction?> GetByTrackIdAsync(string trackId);
    Task<List<UserTransaction>> GetByUserIdAsync(Guid userId);
}

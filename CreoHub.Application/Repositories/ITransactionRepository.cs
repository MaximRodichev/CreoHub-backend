using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface ITransactionRepository : IRepository<Transaction, Guid>
{
    Task<Transaction?> GetByTrackIdAsync(string trackId);
    Task<List<Transaction>> GetByOwnerIdAsync(Guid ownerId);
}

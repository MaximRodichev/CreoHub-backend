using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IUserBalanceRepository : IRepository<UserBalance, Guid>
{
    Task<UserBalance?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Возвращает баланс с пессимистичной блокировкой строки (SELECT … FOR UPDATE).
    /// Должен вызываться внутри явной транзакции через IUnitOfWork.BeginTransactionAsync().
    /// </summary>
    Task<UserBalance?> GetByUserIdForUpdateAsync(Guid userId);
}

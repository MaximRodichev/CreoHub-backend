namespace CreoHub.Application.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Начинает явную транзакцию БД с указанным уровнем изоляции.</summary>
    Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted);

    /// <summary>Коммитит текущую транзакцию и освобождает ресурсы.</summary>
    Task CommitTransactionAsync();

    /// <summary>Откатывает текущую транзакцию и освобождает ресурсы.</summary>
    Task RollbackTransactionAsync();
}
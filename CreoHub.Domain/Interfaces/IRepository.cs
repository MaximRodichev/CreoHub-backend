namespace CreoHub.Domain.Interfaces;

public interface IRepository<T, TKey>
{
    Task<T?> GetByIdAsync(TKey id);
    Task<List<T>> GetByIdsAsync(List<TKey> rangeKeys);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    void Remove(T entity);
    Task<T> UpdateAsync(T entity);
}
using CreoHub.Application.Repositories;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}
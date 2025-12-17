using CreoHub.Application.Exceptions;
using CreoHub.Application.Repositories;
using Microsoft.EntityFrameworkCore;

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
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        { 
            throw new Exception(DbUpdateExceptionFormalize.Formalize(ex));
        }
    }
}
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ContentAccessRepository : IContentAccessRepository
{
    private readonly AppDbContext _db;

    public ContentAccessRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ContentAccess?> GetByIdAsync(Guid id)
    {
        return await _db.ContentAccesses.FirstOrDefaultAsync(ca => ca.Id == id);
    }

    public async Task<List<ContentAccess>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.ContentAccesses
            .Where(ca => rangeKeys.Contains(ca.Id))
            .ToListAsync();
    }

    public async Task<List<ContentAccess>> GetAllAsync()
    {
        return await _db.ContentAccesses.ToListAsync();
    }

    public async Task<ContentAccess> AddAsync(ContentAccess entity)
    {
        return (await _db.ContentAccesses.AddAsync(entity)).Entity;
    }

    public void Remove(ContentAccess entity)
    {
        _db.ContentAccesses.Remove(entity);
    }

    public ContentAccess Update(ContentAccess entity)
    {
        return _db.ContentAccesses.Update(entity).Entity;
    }

    public ContentAccess Attach(ContentAccess entity)
    {
        return _db.ContentAccesses.Attach(entity).Entity;
    }

    public async Task<bool> HasAccessAsync(Guid userId, Guid contentFileId)
    {
        return await _db.ContentAccesses
            .AnyAsync(ca => ca.UserId == userId && ca.ContentFileId == contentFileId);
    }

    public async Task<List<ContentAccess>> GetByUserIdAsync(Guid userId)
    {
        return await _db.ContentAccesses
            .Where(ca => ca.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<ContentAccess>> GetByOrderIdAsync(Guid orderId)
    {
        return await _db.ContentAccesses
            .Where(ca => ca.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<List<ContentAccess>> GetUserFilesAsync(Guid userId)
    {
        return await _db.ContentAccesses
            .Where(ca => ca.UserId == userId)
            .Include(ca => ca.ContentFile)
                .ThenInclude(cf => cf.Product)
                    .ThenInclude(p => p.MediaProducts)
                        .ThenInclude(m => m.StorageObject)
            .OrderBy(ca => ca.ContentFile.ProductId)
            .ThenBy(ca => ca.GrantedAt)
            .ToListAsync();
    }
}

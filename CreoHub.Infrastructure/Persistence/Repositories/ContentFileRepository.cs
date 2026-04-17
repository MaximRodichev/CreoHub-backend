using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ContentFileRepository : IContentFileRepository
{
    private readonly AppDbContext _db;

    public ContentFileRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ContentFile?> GetByIdAsync(Guid id)
    {
        return await _db.ContentFiles.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<ContentFile>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.ContentFiles
            .Where(c => rangeKeys.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<List<ContentFile>> GetAllAsync()
    {
        return await _db.ContentFiles.ToListAsync();
    }

    public async Task<ContentFile> AddAsync(ContentFile entity)
    {
        return (await _db.ContentFiles.AddAsync(entity)).Entity;
    }

    public void Remove(ContentFile entity)
    {
        _db.ContentFiles.Remove(entity);
    }

    public ContentFile Update(ContentFile entity)
    {
        return _db.ContentFiles.Update(entity).Entity;
    }

    public ContentFile Attach(ContentFile entity)
    {
        return _db.ContentFiles.Attach(entity).Entity;
    }

    public async Task<List<ContentFile>> GetByProductIdAsync(int productId)
    {
        return await _db.ContentFiles
            .Where(c => c.ProductId == productId)
            .ToListAsync();
    }

    public async Task<List<ContentFile>> GetByStorageObjectIdAsync(Guid storageObjectId)
    {
        return await  _db.ContentFiles
            .Where(c => c.StorageObjectId == storageObjectId)
            .ToListAsync();
    }

    public async Task<List<ContentFile>> GetByProductIdsAsync(IEnumerable<int> productIds)
    {
        return await _db.ContentFiles
            .AsNoTracking()
            .Where(cf => productIds.Contains(cf.ProductId))
            .ToListAsync();
    }

    public async Task<ContentFile?> GetByIdWithStorageAsync(Guid id)
    {
        return await _db.ContentFiles
            .Include(cf => cf.StorageObject)
            .FirstOrDefaultAsync(cf => cf.Id == id);
    }
}

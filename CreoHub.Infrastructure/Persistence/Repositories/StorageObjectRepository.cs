using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class StorageObjectRepository : IStorageObjectRepository
{
    private readonly AppDbContext _db;

    public StorageObjectRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<StorageObject?> GetByIdAsync(Guid id)
    {
        return await  _db.StorageObjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<StorageObject>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public async Task<List<StorageObject>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<StorageObject> AddAsync(StorageObject entity)
    {
        return (await _db.AddAsync(entity)).Entity;
    }

    public void Remove(StorageObject entity)
    {
        _db.StorageObjects.Remove(entity);
    }

    public StorageObject Update(StorageObject entity)
    {
        throw new NotImplementedException();
    }

    public StorageObject Attach(StorageObject entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<StorageObjectResponseDTO>> GetAllByShopId(Guid shopId)
    {
        var query = _db.StorageObjects.AsNoTracking()
            .Where(x => x.OwnerId == shopId)
            .Select(x => new StorageObjectResponseDTO
            {
                FileName = x.FileName,
                FileSize = x.FileSize,
                FileType = x.FileType,
                Id = x.Id,
                Key = x.Key,
                MimeType = x.MimeType,
            });
        
        return await query.ToListAsync();
    }
}
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
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
        return await  _db.StorageObjects.Include(x=> x.MediaProduct).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<StorageObject>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.StorageObjects
            .AsNoTracking()
            .Where(x => rangeKeys.Contains(x.Id))
            .ToListAsync();
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
       return _db.StorageObjects.Update(entity).Entity;
    }

    public StorageObject Attach(StorageObject entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<StorageObjectResponseDTO>> GetAllByShopId(Guid shopId)
    {
        var rawData = await _db.StorageObjects
            .AsNoTracking()
            .Where(x => x.OwnerId == shopId)
            .Select(x => new 
            {
                Storage = x,
                // Получаем данные из файлов
                Files = x.ContentFiles.Select(cf => new LinkedProductInfo
                { 
                    ProductId = cf.ProductId.ToString(), 
                    ProductName = cf.Product.Name 
                }).ToList(),
        
                // Получаем данные из MediaProduct
                HasMedia = x.MediaProduct != null,
                MediaId = x.MediaProduct != null ? x.MediaProduct.ProductId.ToString() : null,
                MediaName = x.MediaProduct != null ? x.MediaProduct.Product.Name : null
            })
            .ToListAsync();

        var response = rawData.Select(x => new StorageObjectResponseDTO
        {
            Id = x.Storage.Id,
            Key = x.Storage.Key,
            MimeType = x.Storage.MimeType,
            FileSize = x.Storage.FileSize,
            FileName = x.Storage.FileName,
            FileType = x.Storage.FileType,
            UploadedAt = x.Storage.UploadedAt,
            IsSystemLocked = x.Storage.IsSystemLocked,

            LinkedProducts = x.Files
                .Concat(x.HasMedia 
                    ? new[] { new LinkedProductInfo { ProductId = x.MediaId, ProductName = x.MediaName } } 
                    : Enumerable.Empty<LinkedProductInfo>())
                .GroupBy(p => p.ProductId) // Убираем дубликаты по Id
                .Select(g => g.First())
                .ToList()
        }).ToList();
        
        return response;
    }
}
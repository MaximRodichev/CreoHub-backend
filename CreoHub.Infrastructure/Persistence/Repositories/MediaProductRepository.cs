using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class MediaProductRepository : IMediaProductRepository
{
    private readonly AppDbContext _db;
    private IMediaProductRepository _mediaProductRepositoryImplementation;

    public MediaProductRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<MediaProduct?> GetByIdAsync((Guid product, Guid storageObject) id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<MediaProduct>> GetByIdsAsync(List<(Guid product, Guid storageObject)> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public async Task<List<MediaProduct>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<MediaProduct> AddAsync(MediaProduct entity)
    {
        var response = await _db.MediaProducts.AddAsync(entity);
        return response.Entity;
    }

    public void Remove(MediaProduct entity)
    {
        _db.MediaProducts.Remove(entity);
    }

    public MediaProduct Update(MediaProduct entity)
    {
        return _db.MediaProducts.Update(entity).Entity;
    }

    public MediaProduct Attach(MediaProduct entity)
    {
        return _db.MediaProducts.Attach(entity).Entity;
    }

    public async Task<string> GetPreviewKeyByProductId(int productId)
    {
        var response = await _db.MediaProducts.Include(x=>x.StorageObject).FirstOrDefaultAsync(x => x.ProductId == productId);
        
        return response.StorageObject.Key;
    }

    public async Task<Dictionary<int, string?>> GetPreviewKeysByProductIds(IEnumerable<int> productIds)
    {
        return await _db.MediaProducts
            .AsNoTracking()
            .Where(mp => productIds.Contains(mp.ProductId))
            .Select(mp => new { mp.ProductId, mp.StorageObject.Key })
            .GroupBy(x => x.ProductId)
            .Select(g => g.First())
            .ToDictionaryAsync(x => x.ProductId, x => (string?)x.Key);
    }

    public async Task<Dictionary<int, (string? Key, string? ThumbnailKey)>> GetPreviewKeysWithThumbnailByProductIds(IEnumerable<int> productIds)
    {
        var rows = await _db.MediaProducts
            .AsNoTracking()
            .Where(mp => productIds.Contains(mp.ProductId))
            .Select(mp => new
            {
                mp.ProductId,
                Key = mp.StorageObject.Key,
                ThumbnailKey = mp.Thumbnail != null ? mp.Thumbnail.Key : null
            })
            .ToListAsync();

        return rows
            .GroupBy(x => x.ProductId)
            .Select(g => g.First())
            .ToDictionary(x => x.ProductId, x => ((string?)x.Key, (string?)x.ThumbnailKey));
    }

    public async Task<MediaProduct?> GetByStorageObjectIdAsync(Guid storageObjectId)
    {
        return await _db.MediaProducts
            .FirstOrDefaultAsync(mp => mp.StorageObjectId == storageObjectId);
    }

    public async Task<List<MediaProduct>> GetVideosWithoutThumbnailAsync(Guid shopId)
    {
        return await _db.MediaProducts
            .Include(mp => mp.StorageObject)
            .Where(mp =>
                mp.ThumbnailId == null &&
                mp.StorageObject.OwnerId == shopId &&
                mp.StorageObject.MimeType.StartsWith("video/"))
            .ToListAsync();
    }

    public async Task<MediaProduct?> GetByProductAndStorageAsync(int productId, Guid storageObjectId)
    {
        return await _db.MediaProducts
            .FirstOrDefaultAsync(mp =>
                mp.ProductId == productId &&
                mp.StorageObjectId == storageObjectId);
    }

    public async Task<List<MediaProduct>> GetAllVideosWithoutThumbnailAsync()
    {
        return await _db.MediaProducts
            .Include(mp => mp.StorageObject)
            .Where(mp =>
                mp.ThumbnailId == null &&
                mp.StorageObject.MimeType.StartsWith("video/"))
            .ToListAsync();
    }
}
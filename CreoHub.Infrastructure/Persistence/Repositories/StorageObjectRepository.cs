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

    public async Task<StorageObjectsPagedResponseDTO> GetPagedByShopIdAsync(
        Guid    shopId,
        int     page,
        int     pageSize,
        string? search,
        string? type,
        string  sort,
        string  order)
    {
        // ── Агрегаты (один запрос, не зависят от фильтра) ────────────────────
        var allVisible = _db.StorageObjects
            .AsNoTracking()
            .Where(x => x.OwnerId    == shopId
                     && x.FileType   != FileType.Thumbnail
                     && x.FileType   != FileType.Decoration);

        var countAll      = await allVisible.CountAsync();
        var countContent  = await allVisible.CountAsync(x => x.FileType == FileType.Content);
        var countMedia    = await allVisible.CountAsync(x => x.FileType == FileType.Media);
        var countUploaded = await allVisible.CountAsync(x => x.FileType == FileType.Unregistred);
        var countLocked   = await allVisible.CountAsync(x => x.IsSystemLocked);
        var totalSize     = await allVisible.SumAsync(x => (long?)x.FileSize) ?? 0L;

        // ── Основной запрос с фильтром ────────────────────────────────────────
        var query = allVisible;

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.FileName, $"%{search}%"));

        if (type == "image")         query = query.Where(x => x.MimeType.StartsWith("image/"));
        else if (type == "video")    query = query.Where(x => x.MimeType.StartsWith("video/"));
        else if (type == "other")    query = query.Where(x => !x.MimeType.StartsWith("image/") && !x.MimeType.StartsWith("video/"));
        else if (type == "content")  query = query.Where(x => x.FileType == FileType.Content);
        else if (type == "media")    query = query.Where(x => x.FileType == FileType.Media);
        else if (type == "uploaded") query = query.Where(x => x.FileType == FileType.Unregistred);
        else if (type == "archived") query = query.Where(x => x.IsSystemLocked);

        var total = await query.CountAsync();

        // Сортировка
        query = (sort, order) switch
        {
            ("name",  "asc")  => query.OrderBy(x => x.FileName),
            ("name",  "desc") => query.OrderByDescending(x => x.FileName),
            ("size",  "asc")  => query.OrderBy(x => x.FileSize),
            ("size",  "desc") => query.OrderByDescending(x => x.FileSize),
            ("date",  "asc")  => query.OrderBy(x => x.UploadedAt),
            _                 => query.OrderByDescending(x => x.UploadedAt),   // date desc (default)
        };

        var rawData = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Storage  = x,
                Files    = x.ContentFiles.Select(cf => new LinkedProductInfo
                {
                    ProductId   = cf.ProductId.ToString(),
                    ProductName = cf.Product.Name,
                }).ToList(),
                HasMedia  = x.MediaProduct != null,
                MediaId   = x.MediaProduct != null ? x.MediaProduct.ProductId.ToString() : null,
                MediaName = x.MediaProduct != null ? x.MediaProduct.Product.Name : null,
            })
            .ToListAsync();

        var items = rawData.Select(x => new StorageObjectResponseDTO
        {
            Id             = x.Storage.Id,
            Key            = x.Storage.Key,
            MimeType       = x.Storage.MimeType,
            FileSize       = x.Storage.FileSize,
            FileName       = x.Storage.FileName,
            FileType       = x.Storage.FileType,
            UploadedAt     = x.Storage.UploadedAt,
            IsSystemLocked = x.Storage.IsSystemLocked,
            LinkedProducts = x.Files
                .Concat(x.HasMedia
                    ? new[] { new LinkedProductInfo { ProductId = x.MediaId, ProductName = x.MediaName } }
                    : Enumerable.Empty<LinkedProductInfo>())
                .GroupBy(p => p.ProductId)
                .Select(g => g.First())
                .ToList(),
        }).ToList();

        return new StorageObjectsPagedResponseDTO
        {
            Items          = items,
            Total          = total,
            Page           = page,
            PageSize       = pageSize,
            TotalPages     = (int)Math.Ceiling(total / (double)pageSize),
            TotalSizeBytes = totalSize,
            CountAll       = countAll,
            CountContent   = countContent,
            CountMedia     = countMedia,
            CountUploaded  = countUploaded,
            CountLocked    = countLocked,
        };
    }

    public async Task<List<StorageObject>> GetStuckOptimizationJobsAsync(CancellationToken ct = default)
    {
        return await _db.StorageObjects
            .Where(x => x.VideoOptimizationStatus == VideoOptimizationStatus.Queued
                     || x.VideoOptimizationStatus == VideoOptimizationStatus.Processing)
            .ToListAsync(ct);
    }

    public async Task<HashSet<string>> GetAllKeysSetAsync(CancellationToken ct = default)
    {
        var keys = await _db.StorageObjects
            .AsNoTracking()
            .Select(x => x.Key)
            .ToListAsync(ct);

        return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
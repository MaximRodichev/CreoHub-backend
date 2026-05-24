using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<Product>> GetByIdsAsync(List<int> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public Task<List<Product>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<Product> AddAsync(Product entity)
    {
        return (await _db.Products.AddAsync(entity)).Entity;
    }

    public void Remove(Product entity)
    {
        throw new NotImplementedException();
    }

    public Product Update(Product entity)
    {
        return  _db.Products.Update(entity).Entity;
        
    }

    public async Task<(List<ProductViewDTO>, int)> GetProductsByFilters(FiltersDto filters)
    {
        var query = _db.Products.AsNoTracking();

        // 1. Фильтрация
        if (filters.ShopId.HasValue)
            query = query.Where(x => x.OwnerId == filters.ShopId);

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search));
        }

        if (filters.Tags != null && filters.Tags.Any())
            query = query.Where(x => x.Tags.Any(t => filters.Tags.Contains(t.Name)));

        if (filters.ProductType.HasValue)
            query = query.Where(x => x.ProductType == filters.ProductType.Value);

        query = query.Where(x=>x.ProductStatus == ProductStatus.Active);

        // Считаем общее количество до пагинации
        var totalCount = await query.CountAsync();

        // 2. Определяем полностью купленные продукты (для сортировки в конец)
        List<int> fullyOwnedIds = new();
        if (filters.UserId.HasValue)
        {
            // Файлы, которыми владеет пользователь
            var ownedFileIds = await _db.ContentAccesses
                .Where(ca => ca.UserId == filters.UserId.Value)
                .Select(ca => ca.ContentFileId)
                .ToListAsync();

            if (ownedFileIds.Count > 0)
            {
                // Продукты, у которых ВСЕ файлы куплены (owned == total)
                fullyOwnedIds = await _db.ContentFiles
                    .GroupBy(cf => cf.ProductId)
                    .Where(g => g.Count(cf => ownedFileIds.Contains(cf.Id)) == g.Count())
                    .Select(g => g.Key)
                    .ToListAsync();
            }
        }

        // 3. Сортировка: купленные ВСЕГДА в конец (первичный ключ),
        //    выбранный критерий — внутри каждой группы (вторичный ключ).
        IOrderedQueryable<Product> sortedQuery;

        if (fullyOwnedIds.Count > 0)
        {
            var byOwnership = query.OrderBy(p => fullyOwnedIds.Contains(p.Id) ? 1 : 0);
            sortedQuery = filters.SortOrder switch
            {
                SortOrder.AscendingPrice  => byOwnership.ThenBy(x => x.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()),
                SortOrder.DescendingPrice => byOwnership.ThenByDescending(x => x.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()),
                SortOrder.Popularity      => byOwnership.ThenByDescending(x => x.OrderItems.Count),
                SortOrder.Latests         => byOwnership.ThenByDescending(x => x.CreatedAt),
                SortOrder.Oldest          => byOwnership.ThenBy(x => x.CreatedAt),
                _                         => byOwnership.ThenBy(x => x.Id)
            };
        }
        else
        {
            sortedQuery = filters.SortOrder switch
            {
                SortOrder.AscendingPrice  => query.OrderBy(x => x.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()),
                SortOrder.DescendingPrice => query.OrderByDescending(x => x.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()),
                SortOrder.Popularity      => query.OrderByDescending(x => x.OrderItems.Count),
                SortOrder.Latests         => query.OrderByDescending(x => x.CreatedAt),
                SortOrder.Oldest          => query.OrderBy(x => x.CreatedAt),
                _                         => query.OrderBy(x => x.Id)
            };
        }

        // 4. Пагинация и финальная проекция
        var items = await sortedQuery
            .Include(x=>x.BundleItems)
                .ThenInclude(x=>x.Product)
                .ThenInclude(x=>x.Prices)
            .Skip(filters.Page * filters.PageSize)
            .Take(filters.PageSize)
            .Select(x => new ProductViewDTO
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Price =  x.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault(),
                isHotProduct = x.OrderItems.Count > 6,
                Date = x.CreatedAt,
                ProductType = x.ProductType,
                PriceWithoutDiscount = x.BundleItems.Select(y=>y.Product.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()).Sum(),
                PreviewKey = x.MediaProducts
                    .OrderBy(m => m.SortOrder)
                    .Select(m => m.StorageObject.Key)
                    .FirstOrDefault(),
                PreviewThumbnailKey = x.MediaProducts
                    .OrderBy(m => m.SortOrder)
                    .Select(m => m.Thumbnail != null ? m.Thumbnail.Key : null)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ProductInfoDTO> GetProductInfoById(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(x => (x.ProductStatus == ProductStatus.Active || x.ProductStatus == ProductStatus.Archived) && x.Id == id)
            .Select(x => new ProductInfoDTO
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                Date = x.CreatedAt,
                Price = x.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault(),
                ShopId = x.Owner.Id,
                isHotProduct = x.OrderItems.Count > 6,
                ShopName = x.Owner.Name,
                Tags = x.Tags.Select(t => t.Name).ToList(),
                ProductType = x.ProductType,
                ProductStatus = x.ProductStatus,
                inBundleProducts = x.BundleItems.Select(b => new ProductShortInfoDTO
                {
                    Name = b.Product.Name,
                    Id = b.ProductId,
                    Price = b.Product.Prices
                        .OrderByDescending(p => p.Date)
                        .Select(p => p.Value)
                        .FirstOrDefault(),
                }).ToList(),
                // Публичный API: только активные (не архивные) контент файлы
                ContentFileInfos = x.ContentFiles
                    .Where(cf => cf.ArchivedAt == null)
                    .Select(cf => new ContentFileInfo()
                    {
                        Id              = cf.Id,
                        StorageObjectId = cf.StorageObjectId,
                        PreviewName     = cf.PreviewName,
                        PriceWeight     = cf.PriceWeight,
                        IsArchived      = false,
                    }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (product == null) return null;

        var ownMedia = await _db.MediaProducts
            .Where(m => m.ProductId == product.Id)
            .OrderBy(m => m.SortOrder)
            .Select(m => new StorageObjectViewDTO
            {
                Id = m.StorageObjectId,
                Key = m.StorageObject.Key,
                ThumbnailKey = m.Thumbnail != null ? m.Thumbnail.Key : null
            })
            .ToListAsync();

        var directBundleIds = product.inBundleProducts.Select(x => x.Id).ToList();
        var nestedBundleItemIds = directBundleIds.Count > 0
            ? await _db.Products
                .Where(p => directBundleIds.Contains(p.Id) && p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(bi => bi.ProductId))
                .ToListAsync()
            : new List<int>();

        var allBundleIds = directBundleIds.Concat(nestedBundleItemIds).Distinct().ToList();
        var bundleMedia = allBundleIds.Count > 0
            ? await _db.MediaProducts
                .Where(m => allBundleIds.Contains(m.ProductId))
                .OrderBy(m => m.SortOrder)
                .Select(m => new StorageObjectViewDTO
                {
                    Id = m.StorageObjectId,
                    Key = m.StorageObject.Key,
                    ThumbnailKey = m.Thumbnail != null ? m.Thumbnail.Key : null
                })
                .ToListAsync()
            : new List<StorageObjectViewDTO>();

        product.MediaViews = ownMedia.Concat(bundleMedia).ToList();
        return product;
    }

    public async Task<Product> GetProductById(int id)
    {
        return (await _db.Products
            .Include(x=>x.Prices)
            .Include(x=>x.MediaProducts)
            .Include(x=>x.Tags)
            .FirstOrDefaultAsync(x => x.Id == id));
    }

    public Task<Guid> GetShopIdByProductId(int id)
    {
        return _db.Products
            .Where(p => p.Id == id)
            .Select(p => p.OwnerId)
            .FirstOrDefaultAsync();
    }

    public async Task<ProductInfoDTO> GetProductByName(string name)
    {
        // Ищем сначала по Slug (основной путь), потом по Name (обратная совместимость)
        var product = await _db.Products
            .AsNoTracking()
            .Where(x => (x.ProductStatus == ProductStatus.Active || x.ProductStatus == ProductStatus.Archived)
                        && (x.Slug == name || x.Name == name))
            .Select(x => new ProductInfoDTO
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                Date = x.CreatedAt,
                Price = x.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault(),
                ShopId = x.Owner.Id,
                isHotProduct = x.OrderItems.Count > 6,
                ShopName = x.Owner.Name,
                Tags = x.Tags.Select(t => t.Name).ToList(),
                ProductType = x.ProductType,
                ProductStatus = x.ProductStatus,
                inBundleProducts = x.BundleItems.Select(b => new ProductShortInfoDTO
                {
                    Name = b.Product.Name,
                    Id = b.ProductId,
                    Price = b.Product.Prices
                        .OrderByDescending(p => p.Date)
                        .Select(p => p.Value)
                        .FirstOrDefault(),
                }).ToList(),
                // Публичный API: только активные контент файлы
                ContentFileInfos = x.ContentFiles
                    .Where(cf => cf.ArchivedAt == null)
                    .Select(cf => new ContentFileInfo()
                    {
                        Id              = cf.Id,
                        StorageObjectId = cf.StorageObjectId,
                        PreviewName     = cf.PreviewName,
                        PriceWeight     = cf.PriceWeight,
                        IsArchived      = false,
                    }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (product == null) return null;

        // Медиа самого продукта
        var ownMedia = await _db.MediaProducts
            .Where(m => m.ProductId == product.Id)
            .OrderBy(m => m.SortOrder)
            .Select(m => new StorageObjectViewDTO
            {
                Id = m.StorageObjectId,
                Key = m.StorageObject.Key,
                ThumbnailKey = m.Thumbnail != null ? m.Thumbnail.Key : null
            })
            .ToListAsync();

        // Медиа бандл продуктов — рекурсивно собираем все вложенные ID
        var directBundleIds = product.inBundleProducts.Select(x => x.Id).ToList();

        // Для каждого вложенного продукта, который сам является бандлом, берём его items
        var nestedBundleItemIds = directBundleIds.Count > 0
            ? await _db.Products
                .Where(p => directBundleIds.Contains(p.Id) && p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(bi => bi.ProductId))
                .ToListAsync()
            : new List<int>();

        var allBundleIds = directBundleIds
            .Concat(nestedBundleItemIds)
            .Distinct()
            .ToList();

        var bundleMedia = allBundleIds.Count > 0
            ? await _db.MediaProducts
                .Where(m => allBundleIds.Contains(m.ProductId))
                .OrderBy(m => m.SortOrder)
                .Select(m => new StorageObjectViewDTO
                {
                    Id = m.StorageObjectId,
                    Key = m.StorageObject.Key,
                    ThumbnailKey = m.Thumbnail != null ? m.Thumbnail.Key : null
                })
                .ToListAsync()
            : new List<StorageObjectViewDTO>();

        product.MediaViews = ownMedia.Concat(bundleMedia).ToList();

        return product;
    }

    public async Task<ProductAnalyticsDTO> GetProductAnalyticsById(int id)
    {
        // 1. Fetch raw data into an anonymous object
        var rawData = await _db.Products
            .AsNoTracking()
            .Include(p=>p.BundleItems)
                .ThenInclude(bi => bi.Product)
                .ThenInclude(p => p.Prices)
            .Include(x=>x.MediaProducts)
                .ThenInclude(x=>x.StorageObject)
            .Include(x=>x.MediaProducts)
                .ThenInclude(x=>x.Thumbnail)
            .Include(x=>x.OrderItems)
                .ThenInclude(x=>x.Order)
                .ThenInclude(x=>x.Customer)
            .Include(x=>x.ContentFiles)
                .ThenInclude(cf => cf.StorageObject)
            .Where(x => x.Id == id)
            .Select(x => new 
            {
                x.OwnerId,
                x.Id,
                CountSells = x.OrderItems.Count,
                // Max() is translatable to SQL
                LastSellDate = (DateTime?)x.OrderItems.Select(y => y.Order.OrderDate).Max(),
                // Project the collections into simple enumerables EF can handle
                PriceList = x.Prices.Select(p => new { p.Date, p.Value }).ToList(),
                SellsList = x.OrderItems.Select(y => y.Order.OrderDate).ToList(),
                x.ProductStatus,
                x.ProductType,
                x.BundleItems,
                x.MediaProducts,
                ContentFile = x.ContentFiles,
                CustomerBuyHistory = x.OrderItems.Select(y => new OrderSellDTO
                {
                    BuyDate      = y.Order.OrderDate,
                    CustomerName = y.Order.Customer.Name,
                    Amount       = y.PriceAtPurchase,
                }).ToList(),
                TotalRevenue = x.OrderItems.Sum(y => y.PriceAtPurchase),
            })
            .FirstOrDefaultAsync();

        if (rawData == null) return null;

        // 2. Map to your DTO in-memory (Client-side)
        return new ProductAnalyticsDTO
        {
            ShopId = rawData.OwnerId,
            ProductId = rawData.Id,
            CountSells = rawData.CountSells,
            LastSellDate = rawData.LastSellDate,
            // Now ToDictionary works because we are in C#-land, not SQL-land
            PriceHistory = rawData.PriceList.ToDictionary(p => p.Date, p => p.Value),
            SellsDateTimes = rawData.SellsList,
            ProductStatus = rawData.ProductStatus,
            ProductType = rawData.ProductType,
            inBundleProducts = rawData.BundleItems.Select(x => new ProductShortInfoDTO()
            {
                Name = x.Product.Name,
                Id = x.ProductId,
                Price = x.Product.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault(),
            }).ToList(),
            MediaViews = rawData.MediaProducts
                .OrderBy(x => x.SortOrder)
                .Select(x=> new StorageObjectViewDTO()
                {
                    Id = x.StorageObjectId,
                    Key = x.StorageObject.Key,
                    ThumbnailKey = x.Thumbnail?.Key
                }).ToList(),
            SellsHistory = rawData.CustomerBuyHistory,
            TotalRevenue = rawData.TotalRevenue,
            // Аналитика (владелец): показываем ВСЕ файлы включая архивные
            ContentFileInfos = rawData.ContentFile.Select(x => new ContentFileInfo()
            {
                Id              = x.Id,
                StorageObjectId = x.StorageObjectId,
                PreviewName     = x.PreviewName,
                PriceWeight     = x.PriceWeight,
                StorageFileName = x.StorageObject?.FileName,
                IsArchived      = x.ArchivedAt.HasValue,
            }).ToList(),
        };
    }

    public Task<List<ProductViewExtendedDTO>> GetProductsExtendedInfo(Guid shopId)
    {
        return _db.Products
            .AsNoTracking()
            .Where(x=>x.OwnerId == shopId)
            .Select(x=>new ProductViewExtendedDTO()
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Price = x.Prices.OrderBy(x=>x.Date).LastOrDefault().Value,
                isHotProduct = x.OrderItems.Count > 6,
                SellsCount = x.OrderItems.Count,
                ProductStatus = x.ProductStatus,
                Date = x.CreatedAt,
                PreviewKey = x.MediaProducts
                    .OrderBy(m => m.SortOrder)
                    .Select(m => m.StorageObject.Key)
                    .FirstOrDefault(),
                PreviewThumbnailKey = x.MediaProducts
                    .OrderBy(m => m.SortOrder)
                    .Select(m => m.Thumbnail != null ? m.Thumbnail.Key : null)
                    .FirstOrDefault()
            }).ToListAsync();
    }

    public async Task<List<ProductShortInfoDTO>> GetProductsNamesByShopId(Guid shopId)
    {
        return await _db.Products
            .Where(x => x.OwnerId == shopId)
            .Select(x => new ProductShortInfoDTO
            {
                Id          = x.Id,
                Name        = x.Name,
                ProductType = x.ProductType,
                Price       = x.Prices
                    .OrderByDescending(pr => pr.Date)
                    .Select(pr => pr.Value)
                    .FirstOrDefault()
            }).ToListAsync();
    }

    public Task<List<Product>> GetProductsByIds(List<int> ids)
    {
        return _db.Products
            .AsNoTracking()
            .Include(x => x.Prices)
            .Include(x => x.ContentFiles)
            .Include(x => x.BundleItems)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    public async Task<List<ContentFile>> GetContentFilesOfProduct(int productId)
    {
        var response = await _db.Products
            .AsNoTracking()
            .Include(x => x.ContentFiles)
            .FirstOrDefaultAsync(x => x.Id == productId);
        
         return response.ContentFiles.ToList();
    }

    public async Task<int> GetProductsCount()
    {
        return await _db.Products.CountAsync();
    }

    public async Task<List<ProductStatsDTO>> GetProductsStatsByShopIdAsync(
        Guid shopId, 
        DateTime? from = null, 
        DateTime? to = null,
        int? limit = null)
    {
        var query = _db.Products
            .Where(p => p.OwnerId == shopId)
            .Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                // Calculate the Revenue metric
                Revenue = p.OrderItems
                    .Where(oi => (!from.HasValue || oi.Order.OrderDate >= from) && 
                                 (!to.HasValue || oi.Order.OrderDate <= to))
                    .Sum(oi => (decimal?)oi.PriceAtPurchase) ?? 0m,
                // Calculate Current Price
                CurrentPrice = p.Prices
                    .OrderByDescending(pr => pr.Date)
                    .Select(pr => pr.Value)
                    .FirstOrDefault(),
                // Calculate Order Count
                OrderCount = p.OrderItems
                    .Count(oi => (!from.HasValue || oi.Order.OrderDate >= from) && 
                                 (!to.HasValue || oi.Order.OrderDate <= to))
            })
            // 1. Sort using the anonymous property (EF knows how to translate this)
            .OrderByDescending(x => x.Revenue)
            // 2. Apply limit while still in SQL
            .Take(limit ?? int.MaxValue)
            // 3. Final projection to your DTO
            .Select(x => new ProductStatsDTO(
                x.Id,
                x.Name,
                x.Revenue,
                x.CurrentPrice,
                x.OrderCount
            ));

        return await query.ToListAsync();
    }

    public async Task<List<Product>> GetBundlesByChildProductIdsAsync(IEnumerable<int> childProductIds)
    {
        var ids = childProductIds.ToList();
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.ProductType == CreoHub.Domain.Types.ProductType.Bundle &&
                        p.BundleItems.Any(bi => ids.Contains(bi.ProductId)))
            .Include(p => p.BundleItems)
            .ToListAsync();
    }

    public Product Attach(Product entity)
    {
        return _db.Products.Attach(entity).Entity;
    }
}
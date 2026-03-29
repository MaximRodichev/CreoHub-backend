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
        return await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
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
        await _db.Products.AddAsync(entity);
        return null;
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
        
        query = query.Where(x=>x.ProductStatus == ProductStatus.Active);

        // Считаем общее количество до пагинации
        var totalCount = await query.CountAsync();

        // 2. Сортировка по ОРИГИНАЛЬНОЙ сущности (до проекции в DTO)
        // Здесь мы используем свойства Product (x.CreatedAt, x.Prices и т.д.)
        query = filters.SortOrder switch
        {
            SortOrder.AscendingPrice => query.OrderBy(x => x.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()),
            SortOrder.DescendingPrice => query.OrderByDescending(x => x.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()),
            SortOrder.Popularity => query.OrderByDescending(x => x.OrderItems.Count),
            SortOrder.Latests => query.OrderByDescending(x => x.CreatedAt), // Сортируем по дате создания в БД
            SortOrder.Oldest => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderBy(x => x.Id)
        };

        // 3. Пагинация и финальная проекция
        var items = await query
            .Include(x=>x.BundleItems)
                .ThenInclude(x=>x.Product)
                .ThenInclude(x=>x.Prices)
            .Skip(filters.Page * filters.PageSize)
            .Take(filters.PageSize)
            .Select(x => new ProductViewDTO
            {
                Id = x.Id,
                Name = x.Name,
                Price =  x.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault(),
                Tags = x.Tags.Select(t => t.Name).ToList(),
                isHotProduct = x.OrderItems.Count > 6,
                Date = x.CreatedAt,
                ProductType = x.ProductType,
                PriceWithoutDiscount = x.BundleItems.Select(y=>y.Product.Prices.OrderByDescending(p => p.Date).Select(p => p.Value).FirstOrDefault()).Sum(),
                PreviewKey = x.MediaProducts.First().StorageObject.Key
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<ProductInfoDTO> GetProductInfoById(int id)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public async Task<ProductInfoDTO> GetProductByName(string name)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(x => x.ProductStatus == ProductStatus.Active && x.Name == name)
            .Select(x => new ProductInfoDTO
            {
                Id = x.Id,
                Name = x.Name,
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
                inBundleProducts = x.BundleItems.Select(b => new ProductShortInfoDTO
                {
                    Name = b.Product.Name,
                    Id = b.ProductId,
                    Price = b.Product.Prices
                        .OrderByDescending(p => p.Date)
                        .Select(p => p.Value)
                        .FirstOrDefault(),
                }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (product == null) return null;

        // Медиа самого продукта
        var ownMedia = await _db.MediaProducts
            .Where(m => m.ProductId == product.Id)
            .Select(m => new StorageObjectViewDTO
            {
                Id = m.StorageObjectId,
                Key = m.StorageObject.Key
            })
            .ToListAsync();

        // Медиа бандл продуктов
        var bundleIds = product.inBundleProducts.Select(x => x.Id).ToList();
        var bundleMedia = await _db.MediaProducts
            .Where(m => bundleIds.Contains(m.ProductId))
            .Select(m => new StorageObjectViewDTO
            {
                Id = m.StorageObjectId,
                Key = m.StorageObject.Key
            })
            .ToListAsync();

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
            .Include(x=>x.OrderItems)
                .ThenInclude(x=>x.Order)
                .ThenInclude(x=>x.Customer)
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
                CustomerBuyHistory = x.OrderItems.Select(y=> new OrderSellDTO(){BuyDate = y.Order.OrderDate, CustomerName= y.Order.Customer.Name}).ToList(),
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
            MediaViews = rawData.MediaProducts.Select(x=> new StorageObjectViewDTO()
            {
                Id = x.StorageObjectId,
                Key = x.StorageObject.Key
            }).ToList(),
            SellsHistory = rawData.CustomerBuyHistory
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
                Price = x.Prices.OrderBy(x=>x.Date).LastOrDefault().Value,
                isHotProduct = x.OrderItems.Count > 6,
                SellsCount = x.OrderItems.Count,
                ProductStatus = x.ProductStatus,
                Tags = x.Tags.Select(t => t.Name).ToList(),
                Date = x.CreatedAt,
                PreviewKey = x.MediaProducts.First().StorageObject.Key
            }).ToListAsync();
    }

    public async Task<List<ProductShortInfoDTO>> GetProductsNamesByShopId(Guid shopId)
    {
        return await _db.Products
            .Where(x=>x.OwnerId == shopId)
            .Select(x => new ProductShortInfoDTO()
        {
            Id = x.Id,
            Name = x.Name,
            Price = x.Prices
                .OrderByDescending(pr => pr.Date)
                .Select(pr => pr.Value)
                .FirstOrDefault()
        }).ToListAsync();
    }

    public Task<List<Product>> GetProductsByIds(List<int> ids)
    {
        return _db.Products.AsNoTracking().Include(x=>x.Prices).Where(x => ids.Contains(x.Id)).ToListAsync();
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

    public Product Attach(Product entity)
    {
        return _db.Products.Attach(entity).Entity;
    }
}
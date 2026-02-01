using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public Task<Product?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public async Task<List<ProductViewDTO>> GetProductsByFilters(FiltersDto filters)
    {
        var query = _db.Products.AsNoTracking(); // Быстрее для чтения

        // 1. Фильтрация
        query = query.Where(x => filters.ShopId == null || x.OwnerId == filters.ShopId);

        if (filters.Tags != null && filters.Tags.Any())
        {
            query = query.Where(x => x.Tags.Any(t => filters.Tags.Contains(t.Name)));
        }

        // 2. Пагинация (обязательно ПОСЛЕ фильтрации и ДО Select для производительности)
        var response = await query
            .OrderBy(x => x.Id) // Пагинация требует сортировки
            .Select(x => new ProductViewDTO
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault(),
                OwnerId = x.OwnerId,
                OwnerName = x.Owner.Name,
                Tags = x.Tags.Select(t => t.Name).ToList(),
                TotalSells = x.OrderItems.Count,
                Date = x.CreatedAt
            })
            .OrderByDescending(x=>x.TotalSells)
            .Skip(filters.Page * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync();

        return response;
    }

    public Task<ProductInfoDTO> GetProductInfoById(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<Product> GetProductById(int id)
    {
        return (await _db.Products.FirstOrDefaultAsync(x => x.Id == id));
    }

    public Task<Guid> GetShopIdByProductId(int id)
    {
        throw new NotImplementedException();
    }

    public Task<List<ProductInfoDTO>> GetProductsInfoByShopId(Guid shopId)
    {
        throw new NotImplementedException();
        return _db.Products
            .AsNoTracking()
            .Where(x=>x.OwnerId == shopId)
            .Select(x=>new ProductInfoDTO()
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Price = x.Prices.OrderBy(x=>x.Date).LastOrDefault().Value,
                ShopId = x.Owner.Id,
                TotalSells = x.OrderItems.Count,
                ShopName = x.Owner.Name
                
            }).ToListAsync();
    }

    public async Task<List<ProductNameDTO>> GetProductsNamesByShopId(Guid shopId)
    {
        return await _db.Products
            .Where(x=>x.OwnerId == shopId)
            .Select(x => new ProductNameDTO()
        {
            Id = x.Id,
            Name = x.Name
        }).ToListAsync();
    }

    public Task<List<Product>> GetProductsByIds(List<int> ids)
    {
        return _db.Products.AsNoTracking().Include(x=>x.Prices).Where(x => ids.Contains(x.Id)).ToListAsync();
    }

    public Product Attach(Product entity)
    {
        return _db.Products.Attach(entity).Entity;
    }
}
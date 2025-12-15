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

    public Task<Product> UpdateAsync(Product entity)
    {
        throw new NotImplementedException();
    }

    public Task<List<ProductViewDTO>> GetProductsByFilters(FiltersDto filters)
    {
        var response = _db.Products
            .Include(x => x.Prices)
            .Include(x => x.Owner)
            .Include(x => x.Tags)
            .Select(x => new ProductViewDTO
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Price = x.Prices.OrderBy(x=>x.Date).LastOrDefault().Value,
            OwnerId = x.OwnerId,
            OwnerName = x.Owner.Name,
            Tags = x.Tags.Select(t => t.Name).ToList()
        }).ToListAsync();

        return response;
    }

    public Task<ProductInfoDTO> GetProductInfoById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Product> GetProductById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Guid> GetShopIdByProductId(int id)
    {
        throw new NotImplementedException();
    }

    public Task<List<ProductInfoDTO>> GetProductsInfoByShopId(Guid shopId)
    {
        throw new NotImplementedException();
    }
}
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ProductBundleRepository : IProductBundleRepository
{
    
    private readonly AppDbContext _db;
    
    public ProductBundleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductBundle?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ProductBundle>> GetByIdsAsync(List<int> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ProductBundle>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<ProductBundle> AddAsync(ProductBundle entity)
    {
        throw new NotImplementedException();
    }

    public void Remove(ProductBundle entity)
    {
        throw new NotImplementedException();
    }

    public ProductBundle Update(ProductBundle entity)
    {
        throw new NotImplementedException();
    }

    public ProductBundle Attach(ProductBundle entity)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> AddRangeAsync(List<ProductBundle> entities)
    {
        await _db.ProductBundles.AddRangeAsync(entities);
        return true;
    }
}
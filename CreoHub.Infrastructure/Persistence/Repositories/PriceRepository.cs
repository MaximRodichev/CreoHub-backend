using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class PriceRepository : IPriceRepository
{
    private readonly AppDbContext _db;

    public PriceRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public Task<Price?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<List<Price>> GetByIdsAsync(List<int> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public Task<List<Price>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<Price> AddAsync(Price entity)
    {
        var existingPrice = await _db.Prices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == entity.ProductId && x.Date == entity.Date);

        if (existingPrice != null)
            return existingPrice;

        return (await _db.Prices.AddAsync(entity)).Entity;
    }

    public void Remove(Price entity)
    {
        throw new NotImplementedException();
    }

    public Price Update(Price entity)
    {
        throw new NotImplementedException();
    }

    public async Task<Price> GetPriceByProductId(int id)
    {
        return await _db.Prices
            .Where(x => x.ProductId == id)
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync();
    }
    
    public Price Attach(Price entity)
    {
        return _db.Prices.Attach(entity).Entity;
    }
}
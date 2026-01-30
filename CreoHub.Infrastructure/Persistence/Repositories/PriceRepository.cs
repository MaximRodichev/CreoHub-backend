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
        Price response = (await _db.Prices.AddAsync(entity)).Entity;
        return response;
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
        return await _db.Prices.OrderBy(x=>x.Date).LastOrDefaultAsync(x=> x.ProductId == id);
    }
}
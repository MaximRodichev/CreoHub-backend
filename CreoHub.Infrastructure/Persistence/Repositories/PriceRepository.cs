using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;

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
        await _db.Prices.AddAsync(entity);
        return null;
    }

    public void Remove(Price entity)
    {
        throw new NotImplementedException();
    }

    public Task<Price> UpdateAsync(Price entity)
    {
        throw new NotImplementedException();
    }

    public Task<Price> GetPriceByProductId(int id)
    {
        throw new NotImplementedException();
    }
}
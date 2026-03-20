using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class MediaProductRepository : IMediaProductRepository
{
    private readonly AppDbContext _db;
    
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
        throw new NotImplementedException();
    }

    public void Remove(MediaProduct entity)
    {
        throw new NotImplementedException();
    }

    public MediaProduct Update(MediaProduct entity)
    {
        throw new NotImplementedException();
    }

    public MediaProduct Attach(MediaProduct entity)
    {
        throw new NotImplementedException();
    }
}
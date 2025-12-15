using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class ShopRepository : IShopRepository
{
    private readonly AppDbContext _db;

    public ShopRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public Task<Shop?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<Shop>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public Task<List<Shop>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<Shop> AddAsync(Shop entity)
    {
        return (await _db.Shops.AddAsync(entity)).Entity;
    }

    public void Remove(Shop entity)
    {
        throw new NotImplementedException();
    }

    public Task<Shop> UpdateAsync(Shop entity)
    {
        throw new NotImplementedException();
    }

    public async Task<Shop> GetByOwnerIdAsync(Guid ownerId)
    {
        return (await _db.Shops.FirstAsync(x => x.OwnerId == ownerId));
    }

    public async Task<List<ShopShortInfoDTO>> GetShopsShortInfoAsync()
    {
        return await _db.Shops
            .Include(x => x.Owner)
            .Select(x => new ShopShortInfoDTO
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CountProducts = x.Products.Count,
                OwnerId = x.OwnerId,
                OwnerName = x.Owner.Name
            })
            .ToListAsync();
    }
    
    public async Task<ShopShortInfoDTO?> GetShopShortInfoAsync(Guid id)
    {
        return await _db.Shops
            .Include(x => x.Owner)
            .Where(x => x.Id == id)
            .Select(x => new ShopShortInfoDTO
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CountProducts = x.Products.Count,
                OwnerId = x.OwnerId,
                OwnerName = x.Owner.Name
            })
            .FirstOrDefaultAsync();
    }

}
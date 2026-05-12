using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
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
        var response = (await _db.Shops.AddAsync(entity)).Entity;
        return response;
    }

    public void Remove(Shop entity)
    {
        throw new NotImplementedException();
    }

    public Shop Update(Shop entity)
    {
        throw new NotImplementedException();
    }

    public async Task<Shop> GetByOwnerIdAsync(Guid ownerId)
    {
        return (await _db.Shops.FirstAsync(x => x.OwnerId == ownerId));
    }

    public async Task<Guid?> GetShopIdByOwnerIdAsync(Guid ownerId)
    {
        var shop = await _db.Shops
            .Where(s => s.OwnerId == ownerId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();
        return shop;
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

    public async Task<ShopStatsDTO> GetShopStatsAsync(Guid shopId, DateTime? from = null, DateTime? to = null)
    {
        // 1. Базовый запрос — только COMPLETED заказы, связанные с этим магазином
        var periodOrdersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed &&
                        o.Items.Any(oi => oi.Product.OwnerId == shopId));

        if (from.HasValue) periodOrdersQuery = periodOrdersQuery.Where(o => o.OrderDate >= from);
        if (to.HasValue) periodOrdersQuery = periodOrdersQuery.Where(o => o.OrderDate <= to);

        // 2. Статистика за период (один запрос)
        var statsResult = await periodOrdersQuery
            .GroupBy(o => 1)
            .Select(g => new
            {
                Revenue = g.Sum(o => o.Price),
                OrdersCount = g.Count(),
                ClientsCount = g.Select(o => o.CustomerId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        var revenueData = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed &&
                        o.Items.Any(oi => oi.Product.OwnerId == shopId))
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                TotalSum = g.Sum(o => o.Price),
                TransactionsCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

// Превращаем в словарь для фронтенда
        var revenueHistory = revenueData.ToDictionary(
            x => x.Date.ToString("yyyy-MM-dd"), 
            x => x.TotalSum
        );

        var productsCount = await _db.Products.CountAsync(p => p.OwnerId == shopId);

        return new ShopStatsDTO(
            TotalRevenue: statsResult?.Revenue ?? 0,
            TotalOrders: statsResult?.OrdersCount ?? 0,
            TotalProducts: productsCount,
            TotalClients: statsResult?.ClientsCount ?? 0,
            RevenuePerMonth: revenueHistory // Список выручки по месяцам
        );
    }

    public Shop Attach(Shop entity)
    {
        return _db.Shops.Attach(entity).Entity;
    }
}
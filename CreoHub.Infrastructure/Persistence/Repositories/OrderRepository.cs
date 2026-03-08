using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public Task<Order?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
    
    public async Task<List<Order>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.Orders
            .Where(o => rangeKeys.Contains(o.Id))
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return _db.Orders.ToList();
    }

    public async Task<Order> AddAsync(Order entity)
    {
        return (await _db.Orders.AddAsync(entity)).Entity;
    }

    public void Remove(Order entity)
    {
        throw new NotImplementedException();
    }

    public Order Update(Order entity)
    {
        throw new NotImplementedException();
    }

    public async Task<OrderFullInfoDTO> GetOrderInfoById(Guid id)
    {
        var response =  await _db.Orders
            .Select(x=> new OrderFullInfoDTO()
            {
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                Date =  x.OrderDate,
                Status = x.Status.ToString(),
                ProductItems = x.Items.Select(y=> new ProductOrderInfoDTO()
                {
                    Id = y.Id,
                    Name = y.Product.Name,
                    Price = y.PriceAtPurchase,
                    ShopId = y.Product.OwnerId,
                    ShopName = y.Product.Owner.Name
                }).ToList(),
                Id = x.Id,
            })
            .FirstOrDefaultAsync(o => o.Id == id);

        return response;
    }

    public async Task<List<OrderShortInfoDTO>> GetOrdersShortInfoByShopIdAsync(
        Guid shopId, 
        DateTime? from = null, 
        DateTime? to = null, 
        int? limit = null)
    {
        // 1. Создаем базовый запрос с фильтром по магазину
        var query = _db.Orders
            .Where(x => x.Items.Any(i => i.Product.OwnerId == shopId));

        // 2. Применяем фильтрацию по датам (если они переданы)
        if (from.HasValue)
        {
            query = query.Where(x => x.OrderDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.OrderDate <= to.Value);
        }

        // 3. Сортируем и выбираем только нужные поля (Проекция)
        var projectedQuery = query
            .OrderByDescending(x => x.OrderDate)
            .Select(order => new OrderShortInfoDTO
            {
                Id = order.Id,
                CustomerName = order.Customer.Name,
                OrderDate = order.OrderDate,
                Price = order.Price,
                // Используем Select без ToList() внутри проекции для EF
                ProductNames = order.Items.Select(i => i.Product.Name).ToList(),
                Status = order.Status.ToString(),
            });

        // 4. Применяем лимит в самом конце
        if (limit.HasValue)
        {
            projectedQuery = projectedQuery.Take(limit.Value);
        }

        return await projectedQuery.ToListAsync();
    }
    
    public Order Attach(Order entity)
    {
        return _db.Orders.Attach(entity).Entity;
    }

}
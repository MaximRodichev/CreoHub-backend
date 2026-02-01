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

    public async Task<List<OrderShortInfoDTO>> GetOrdersShortInfoByShopId(Guid shopId)
    {
        return await _db.Orders.Where(x => x.Items.Any(x=>x.Product.OwnerId==shopId))
            .Select(order => new OrderShortInfoDTO()
            {
                Id = order.Id,
                CustomerName = order.Customer.Name,
                OrderDate = order.OrderDate,
                Price = order.Price,
                ProductNames = order.Items.Select(x=>x.Product.Name).ToList(),
                Status = order.Status.ToString(),
            }).ToListAsync();
    }
    
    public Order Attach(Order entity)
    {
        return _db.Orders.Attach(entity).Entity;
    }

}
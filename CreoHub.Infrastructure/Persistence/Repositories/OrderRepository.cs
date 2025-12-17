using CreoHub.Application.DTO.OrderDTOs;
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

    public Task<Order> UpdateAsync(Order entity)
    {
        throw new NotImplementedException();
    }

    public async Task<OrderFullInfoDTO> GetOrderInfoById(Guid id)
    {
        var response =  await _db.Orders
            .Include(x=>x.Customer)
            .Include(x=>x.Product)
            .Include(x=>x.Product.Owner)
            .Select(x=> new OrderFullInfoDTO()
            {
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                Date =  x.OrderDate,
                Price = x.Price,
                Status = x.Status.ToString(),
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                Id = x.Id,
                ShopId = x.Product.OwnerId,
                ShopName = x.Product.Owner.Name,
                
            })
            .FirstOrDefaultAsync(o => o.Id == id);

        return response;
    }
}
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

    public Task<List<Order>> GetAllAsync()
    {
        throw new NotImplementedException();
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

    public Task<OrderInfoDTO> GetOrderInfoById(Guid id)
    {
        throw new NotImplementedException();
    }
}
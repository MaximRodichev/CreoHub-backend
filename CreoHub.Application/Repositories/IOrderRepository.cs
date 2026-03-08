using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;
using CreoHub.Domain.Types;

namespace CreoHub.Application.Repositories;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<OrderFullInfoDTO> GetOrderInfoById(Guid id);
    Task<List<OrderShortInfoDTO>> GetOrdersShortInfoByShopIdAsync(Guid shopId, DateTime? from = null, DateTime? to = null, int? limit = null);
}
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;
using CreoHub.Domain.Types;

namespace CreoHub.Application.Repositories;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<List<OrderUserInfoDTO>> GetUserOrders(Guid userIdm, int page, int limit);
    Task<OrderFullInfoDTO> GetOrderInfoById(Guid id);
    Task<List<OrderShortInfoDTO>> GetOrdersShortInfoByShopIdAsync(Guid shopId, DateTime? from = null, DateTime? to = null, int? limit = null);
    Task<Order?> GetByTransactionIdWithItemsAsync(Guid transactionId);
}
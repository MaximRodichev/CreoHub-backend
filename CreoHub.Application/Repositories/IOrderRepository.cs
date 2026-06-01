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
    /// <summary>Заказы в статусе Created, созданные раньше указанного времени.</summary>
    Task<List<Order>> GetStaleCreatedOrdersAsync(DateTime olderThan);

    /// <summary>
    /// Возвращает список (R2Key, DisplayName) файлов для скачивания.
    /// Null — заказ не найден или не принадлежит пользователю или не оплачен.
    /// </summary>
    Task<List<(string Key, string FileName)>?> GetOrderDownloadFilesAsync(Guid orderId, Guid userId);

    /// <summary>
    /// Возвращает названия товаров в оплаченном заказе (для формирования имени ZIP-архива).
    /// Null — заказ не найден / не оплачен / не принадлежит пользователю.
    /// </summary>
    Task<List<string>?> GetOrderProductNamesAsync(Guid orderId, Guid userId);
}
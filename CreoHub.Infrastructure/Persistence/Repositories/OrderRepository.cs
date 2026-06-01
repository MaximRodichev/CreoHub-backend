using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Transaction)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    
    public async Task<List<Order>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        return await _db.Orders
            .Where(o => rangeKeys.Contains(o.Id))
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _db.Orders.ToListAsync();
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

    public async Task<List<OrderUserInfoDTO>> GetUserOrders(Guid userId, int page = 0, int limit = 99)
    {
        var query = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == userId)
            .Select(x => new OrderUserInfoDTO()
            {
                OrderId = x.Id,
                OrderDate = x.OrderDate,
                Status = x.Status,
                Subtotal = x.Subtotal,
                TotalPrice = x.Price,
                PersonalDiscountPercent = x.PersonalDiscountPercent,
                CartDiscountPercent = x.CartDiscountPercent,
                DiscountPercent = x.DiscountPercent,
                DiscountAmount = x.DiscountAmount,
                PaidAt = x.Transaction != null ? x.Transaction.PaidAt : null,
                TxHash = x.Transaction != null ? x.Transaction.TxHash : null,
                TransactionStatus = x.Transaction != null ? x.Transaction.TransactionStatus : null,
                TransactionId = x.Transaction != null ? x.Transaction.Id : null,
                Items = x.Items.Select(y => new OrderItemDTO()
                {
                    PriceAtPurchase = y.PriceAtPurchase,
                    ProductId = y.ProductId,
                    ProductName = y.Product.Name,
                    IsBundle = y.Product.ProductType == ProductType.Bundle,
                    // Для одиночных продуктов — прямые файлы
                    Files = _db.ContentAccesses
                        .Where(ca => ca.OrderId == x.Id && ca.ContentFile.ProductId == y.ProductId)
                        .Select(ca => new PurchasedFileDTO
                        {
                            ContentFileId = ca.ContentFileId,
                            FileName = ca.ContentFile.PreviewName
                        })
                        .ToList(),
                    // Для бандлов — дочерние продукты с файлами
                    BundleItems = y.Product.BundleItems.Select(bi => new BundleOrderItemDTO
                    {
                        ProductId = bi.ProductId,
                        ProductName = bi.Product.Name,
                        Files = _db.ContentAccesses
                            .Where(ca => ca.OrderId == x.Id && ca.ContentFile.ProductId == bi.ProductId)
                            .Select(ca => new PurchasedFileDTO
                            {
                                ContentFileId = ca.ContentFileId,
                                FileName = ca.ContentFile.PreviewName
                            })
                            .ToList()
                    }).ToList()
                }).ToList(),
            })
            .OrderByDescending(x => x.OrderDate)
            .Skip(page * limit)
            .Take(limit)
            .ToListAsync();

        return query;
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

    public async Task<Order?> GetByTransactionIdWithItemsAsync(Guid transactionId)
    {
        // Мы ищем заказ, у которого в связанной транзакции совпадает Id.
        // EF Core сам сделает правильный JOIN под капотом.
        return await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Files)
            .Include(o => o.Transaction)
            .FirstOrDefaultAsync(o => o.Transaction.Id == transactionId);
    }

    public async Task<List<Order>> GetStaleCreatedOrdersAsync(DateTime olderThan)
    {
        return await _db.Orders
            .Where(o => o.Status == OrderStatus.Created && o.OrderDate < olderThan)
            .ToListAsync();
    }

    public async Task<List<(string Key, string FileName)>?> GetOrderDownloadFilesAsync(Guid orderId, Guid userId)
    {
        // Verify order belongs to user and is paid
        var exists = await _db.Orders.AnyAsync(o =>
            o.Id == orderId &&
            o.CustomerId == userId &&
            o.Status == OrderStatus.Completed);

        if (!exists) return null;

        var files = await _db.Orders
            .Where(o => o.Id == orderId)
            .SelectMany(o => o.Items)
            .SelectMany(i => i.Files)
            .Select(f => new
            {
                Key      = f.ContentFile.StorageObject.Key,
                FileName = f.ContentFile.PreviewName,
            })
            .ToListAsync();

        return files.Select(f => (f.Key, f.FileName)).ToList();
    }
}
using CreoHub.Application.DTO.ShopRequestDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;

namespace CreoHub.Application.Repositories;

public interface IShopRequestRepository
{
    /// <summary>Добавляет запрос в контекст (сохранение — через IUnitOfWork в хендлере).</summary>
    Task<ShopRequest> AddAsync(ShopRequest entity, CancellationToken ct = default);

    /// <summary>Tracked-сущность для мутаций (Reply/Decline).</summary>
    Task<ShopRequest?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Список запросов магазина (продавец), опц. фильтр по статусу, пагинация.</summary>
    Task<List<ShopRequestDTO>> GetByShopIdAsync(
        Guid shopId, ShopRequestStatus? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Список предложений покупателя (с названием магазина), пагинация.</summary>
    Task<List<MyShopRequestDTO>> GetByBuyerIdAsync(
        Guid buyerUserId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Кол-во New-запросов магазина (для бейджа).</summary>
    Task<int> CountNewByShopIdAsync(Guid shopId, CancellationToken ct = default);

    /// <summary>Есть ли у покупателя ещё необработанный (New) запрос в этот магазин (анти-спам).</summary>
    Task<bool> HasOpenRequestAsync(Guid buyerUserId, Guid shopId, CancellationToken ct = default);

    /// <summary>Сколько запросов покупатель отправил с момента since (дневной лимит).</summary>
    Task<int> CountByBuyerSinceAsync(Guid buyerUserId, DateTime since, CancellationToken ct = default);
}

using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IShopTransactionRepository : IRepository<ShopTransaction, Guid>
{
    Task<ShopTransaction?> GetByTrackIdAsync(string trackId);
    Task<List<ShopTransaction>> GetByShopIdAsync(Guid shopId);
    Task<ShopTransaction?> GetLastWithdrawalAsync(Guid shopId);
}

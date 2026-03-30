using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IShopBalanceRepository : IRepository<ShopBalance, Guid>
{
    Task<ShopBalance?> GetByShopIdAsync(Guid shopId);
}

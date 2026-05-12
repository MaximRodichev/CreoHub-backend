using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IShopRepository : IRepository<Shop, Guid>
{
    Task<Shop> GetByOwnerIdAsync(Guid ownerId);
    Task<Guid?> GetShopIdByOwnerIdAsync(Guid ownerId);
    Task<List<ShopShortInfoDTO>> GetShopsShortInfoAsync();
    Task<ShopShortInfoDTO> GetShopShortInfoAsync(Guid guid);
    Task<ShopStatsDTO> GetShopStatsAsync(Guid guid, DateTime? from = null, DateTime? to = null);

}
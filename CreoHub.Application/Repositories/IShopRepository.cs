using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IShopRepository : IRepository<Shop, Guid>
{
    Task<Shop> GetByOwnerIdAsync(Guid ownerId);
    Task<List<ShopShortInfoDTO>> GetShopsShortInfoAsync();
    Task<ShopShortInfoDTO> GetShopShortInfoAsync(Guid guid);
}
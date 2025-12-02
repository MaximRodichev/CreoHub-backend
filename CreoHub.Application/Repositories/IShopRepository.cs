using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IShopRepository : IRepository<Shop, Guid>
{
    Task<Shop> GetByOwnerIdAsync(Guid ownerId);
}
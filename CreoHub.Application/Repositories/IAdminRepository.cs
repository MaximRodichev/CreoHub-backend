using CreoHub.Application.DTO.AdminDTOs;

namespace CreoHub.Application.Repositories;

public interface IAdminRepository
{
    Task<List<AdminUserDto>>        GetAllUsersAsync(CancellationToken ct = default);
    Task<AdminUserDetailDto?>       GetUserDetailAsync(Guid userId, CancellationToken ct = default);
    Task<List<AdminShopDto>>        GetAllShopsAsync(CancellationToken ct = default);
    Task<List<AdminProductNameDto>> GetAllProductNamesAsync(CancellationToken ct = default);
}

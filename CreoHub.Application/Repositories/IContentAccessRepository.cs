using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IContentAccessRepository : IRepository<ContentAccess, Guid>
{
    Task<bool> HasAccessAsync(Guid userId, Guid contentFileId);
    Task<List<ContentAccess>> GetByUserIdAsync(Guid userId);
    Task<List<ContentAccess>> GetByOrderIdAsync(Guid orderId);
}

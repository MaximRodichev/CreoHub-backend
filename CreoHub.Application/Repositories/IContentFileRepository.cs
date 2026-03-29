using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IContentFileRepository : IRepository<ContentFile, Guid>
{
    Task<List<ContentFile>> GetByProductIdAsync(int productId);
}

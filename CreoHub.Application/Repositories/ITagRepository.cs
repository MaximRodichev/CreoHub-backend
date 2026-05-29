using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface ITagRepository : IRepository<Tag, int>
{
    public Task<List<Tag>> GetByNamesAsync(List<string> names);
    public Task<bool> ExistsByNameAsync(string name);
    public Task<List<TagStatsDTO>> GetTagStatsByShopAsync(Guid shopId, DateTime? from = null, DateTime? to = null, int? limit = null);
}
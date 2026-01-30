using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface ITagRepository : IRepository<Tag, int>
{
    public Task<List<Tag>> GetByNamesAsync(List<string> names);
}
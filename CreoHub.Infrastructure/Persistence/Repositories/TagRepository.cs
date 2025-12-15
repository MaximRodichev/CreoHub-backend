using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private AppDbContext _db;

    public TagRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public Task<Tag?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Tag>> GetByIdsAsync(List<int> rangeKeys)
    {
        return await _db.Tags.Where(x=>rangeKeys.Contains(x.Id)).ToListAsync();
    }

    public async Task<List<Tag>> GetAllAsync()
    {
        return await _db.Tags.ToListAsync();
    }

    public async Task<Tag> AddAsync(Tag entity)
    {
       return (await _db.Tags.AddAsync(entity)).Entity;
    }

    public void Remove(Tag entity)
    {
        throw new NotImplementedException();
    }

    public Task<Tag> UpdateAsync(Tag entity)
    {
        throw new NotImplementedException();
    }
}
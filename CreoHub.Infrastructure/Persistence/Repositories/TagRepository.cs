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

    public Tag Update(Tag entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Tag>> GetByNamesAsync(List<string> names)
    {
        // 1. Получаем теги, которые уже существуют в базе
        var existingTags = await _db.Tags
            .Where(t => names.Contains(t.Name))
            .ToListAsync();

        // 2. Вычисляем, каких имен не хватает
        var existingNames = existingTags.Select(t => t.Name).ToList();
        var missingNames = names.Except(existingNames).ToList();

        // 3. Создаем новые объекты для недостающих имен
        if (missingNames.Any())
        {
            var newTags = missingNames.Select(name => new Tag 
            { 
                Name = name 
            }).ToList();

            _db.Tags.AddRange(newTags);
            await _db.SaveChangesAsync();

            // Добавляем созданные теги к списку существующих для возврата
            existingTags.AddRange(newTags);
        }

        return existingTags;
    }

    public Tag Attach(Tag entity)
    {
        return _db.Tags.Attach(entity).Entity;
    }
}
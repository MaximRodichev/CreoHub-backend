using CreoHub.Application.DTO.StatsDTOs;
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
            var newTags = missingNames.Select(name => new Tag(name)).ToList();

            _db.Tags.AddRange(newTags);
            await _db.SaveChangesAsync();

            // Добавляем созданные теги к списку существующих для возврата
            existingTags.AddRange(newTags);
        }

        return existingTags;
    }

    public async Task<List<TagStatsDTO>> GetTagStatsByShopAsync(Guid shopId, DateTime? from = null, DateTime? to = null, int? limit = null)
    {
        var query = _db.Tags
            .Where(t => t.Products.Any(p => p.OwnerId == shopId))
            .Select(tag => new TagStatsDTO
            {
                TagId = tag.Id,
                TagName = tag.Name,

                // Количество товаров этого магазина с данным тегом
                TagProductsCount = tag.Products
                    .Count(p => p.OwnerId == shopId),

                // Общее кол-во продаж (штук) товаров с этим тегом в этом магазине
                TagItemsSold = tag.Products
                    .Where(p => p.OwnerId == shopId)
                    .SelectMany(p => p.OrderItems)
                    .Count(oi => (!from.HasValue || oi.Order.OrderDate >= from) &&
                                 (!to.HasValue || oi.Order.OrderDate <= to)),

                TagOrdersCount = tag.Products
                    .Where(p => p.OwnerId == shopId)
                    .SelectMany(p => p.OrderItems)
                    .Where(oi => (!from.HasValue || oi.Order.OrderDate >= from) &&
                                 (!to.HasValue || oi.Order.OrderDate <= to))
                    .Select(oi => oi.OrderId) // Выбираем ID заказов
                    .Distinct() // Убираем дубликаты
                    .Count(),

                // Суммарная выручка по тегу
                // Используем PriceAtPurchase из OrderItems, так как это историческая цена продажи
                TagRevenue = tag.Products
                    .Where(p => p.OwnerId == shopId)
                    .SelectMany(p => p.OrderItems)
                    .Where(oi => (!from.HasValue || oi.Order.OrderDate >= from) &&
                                 (!to.HasValue || oi.Order.OrderDate <= to))
                    .Sum(oi => (decimal?)oi.PriceAtPurchase) ?? 0m
            })
            .OrderByDescending(t => t.TagRevenue)
            .AsNoTracking();
        
        if (limit.HasValue)
        {
            return await query.Take(limit.Value).ToListAsync();
        }
        return await query.ToListAsync();
    }

    public Tag Attach(Tag entity)
    {
        return _db.Tags.Attach(entity).Entity;
    }
}
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence.Repositories;

public class CartRepository : ICartRepository
{
    
    private readonly AppDbContext _db;
    
    public CartRepository(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<Cart?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Cart>> GetByIdsAsync(List<Guid> rangeKeys)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Cart>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<Cart> AddAsync(Cart entity)
    {
        throw new NotImplementedException();
    }

    public void Remove(Cart entity)
    {
        throw new NotImplementedException();
    }

    public Cart Update(Cart entity)
    {
        throw new NotImplementedException();
    }

    public Cart Attach(Cart entity)
    {
        throw new NotImplementedException();
    }

    public async Task<Cart> GetFullCartAsync(Guid userId)
    {
        var response = await _db
            .Carts
            .AsNoTracking()
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                    .ThenInclude(p => p.BundleItems)
                        .ThenInclude(bi => bi.Product)
                            .ThenInclude(p => p.Prices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        return response;
    }

    public async Task<Cart> GetByUserIdAsync(Guid userId)
    {
        return await _db.Carts.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<CartItem> GetCartItemByUserAndProduct(Guid userId, int productId)
    {
        var cartId = (await _db.Carts.FirstOrDefaultAsync(x => x.UserId == userId)).Id;
        if (cartId == Guid.Empty)
        {
            throw new Exception("Cart not found");
        }
        return await _db.CartItems.FirstOrDefaultAsync(x => x.CartId == cartId  && x.ProductId == productId);
    }

    public async Task<bool> AddCartItem(CartItem cartItem)
    {
        await _db.CartItems.AddAsync(cartItem);
        return true;
    }

    public async Task<bool> RemoveCartItem(CartItem cartItem)
    {
        _db.CartItems.Remove(cartItem);
        return true;
    }

    public async Task<CartItem?> GetCartItemWithFilesAsync(Guid cartItemId)
    {
        return await _db.CartItems
            .Include(ci => ci.SelectedFiles)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cartId = (await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId))?.Id;
        if (cartId is null) return;

        var items = await _db.CartItems
            .Where(ci => ci.CartId == cartId)
            .ToListAsync();

        _db.CartItems.RemoveRange(items);
    }

    public async Task RemoveCartItemsByProductIdsAsync(Guid userId, IEnumerable<int> productIds)
    {
        var cartId = (await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId))?.Id;
        if (cartId is null) return;

        var productIdSet = productIds.ToHashSet();
        var items = await _db.CartItems
            .Where(ci => ci.CartId == cartId && productIdSet.Contains(ci.ProductId))
            .ToListAsync();

        if (items.Count > 0)
            _db.CartItems.RemoveRange(items);
    }
}
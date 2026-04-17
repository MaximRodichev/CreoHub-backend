using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface ICartRepository : IRepository<Cart, Guid>
{
    public Task<Cart> GetFullCartAsync(Guid userId);
    public Task<Cart> GetByUserIdAsync(Guid userId);
    public Task<CartItem> GetCartItemByUserAndProduct(Guid userId, int productId);
    public Task<bool> AddCartItem(CartItem cartItem);
    public Task<bool> RemoveCartItem(CartItem cartItem);
    /// <summary>CartItem с SelectedFiles (tracking) для обновления набора файлов.</summary>
    public Task<CartItem?> GetCartItemWithFilesAsync(Guid cartItemId);
    /// <summary>Удалить все CartItem из корзины пользователя.</summary>
    public Task ClearCartAsync(Guid userId);
}
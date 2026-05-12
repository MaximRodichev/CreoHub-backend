using System.Security.Claims;
using CreoHub.Application.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

/// <summary>
/// Базовый контроллер для эндпоинтов владельца магазина.
/// ShopId резолвится из БД по UserId — не зависит от JWT-клейма shop_id,
/// поэтому корректно работает даже если токен выпущен до создания шопа.
/// </summary>
public abstract class ShopOwnerControllerBase : ControllerBase
{
    protected Guid UserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Возвращает ShopId владельца из БД.
    /// null — если у пользователя нет шопа.
    /// </summary>
    protected async Task<Guid?> GetShopIdAsync(IShopRepository shopRepository)
        => await shopRepository.GetShopIdByOwnerIdAsync(UserId);

    /// <summary>
    /// Хелпер: резолвит ShopId и сразу возвращает 403 если шопа нет.
    /// Используй как:
    ///   if (!await TryGetShopId(repo, out var shopId)) return _forbidden;
    /// </summary>
    protected async Task<(bool ok, Guid shopId)> TryGetShopId(IShopRepository shopRepository)
    {
        var id = await shopRepository.GetShopIdByOwnerIdAsync(UserId);
        return id.HasValue ? (true, id.Value) : (false, Guid.Empty);
    }
}

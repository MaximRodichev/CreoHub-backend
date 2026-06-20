using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

/// <summary>
/// Публичная отдача og:image карточек товаров. Стабильный URL (без presign-протухания) —
/// его и кэшируют соцсети. JPEG лежит в R2; если ещё не сгенерирован — генерим лениво.
/// </summary>
[ApiController]
[Route("api/og")]
public class OgController : ControllerBase
{
    private readonly IProductOgImageService _og;
    private readonly IStorageService        _storage;
    private readonly IProductRepository     _products;

    public OgController(IProductOgImageService og, IStorageService storage, IProductRepository products)
    {
        _og       = og;
        _storage  = storage;
        _products = products;
    }

    [AllowAnonymous]
    [HttpGet("product/{slug}.jpg")]
    public async Task<IActionResult> ProductOg(string slug, CancellationToken ct)
    {
        var key = _og.OgKeyForSlug(slug);

        if (!await _storage.FileExistsAsync(key))
        {
            // Ленивая генерация — покрывает товары, созданные до фичи.
            ProductInfoDTOId id;
            try { id = await ResolveAsync(slug); }
            catch { return NotFound(); }
            if (id.Id <= 0) return NotFound();

            await _og.GenerateAndStoreAsync(id.Id, ct);
            if (!await _storage.FileExistsAsync(key)) return NotFound();
        }

        var stream = await _storage.OpenReadStreamAsync(key);
        Response.Headers["Cache-Control"] = "public, max-age=3600";
        return File(stream, "image/jpeg");
    }

    private readonly record struct ProductInfoDTOId(int Id);

    private async Task<ProductInfoDTOId> ResolveAsync(string slug)
    {
        var dto = await _products.GetProductByName(slug);
        return new ProductInfoDTOId(dto?.Id ?? 0);
    }
}

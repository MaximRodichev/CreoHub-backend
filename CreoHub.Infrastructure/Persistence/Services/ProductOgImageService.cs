using System.Globalization;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace CreoHub.Infrastructure.Persistence.Services;

public class ProductOgImageService : IProductOgImageService
{
    private readonly IProductRepository       _products;
    private readonly IStorageObjectRepository _storageObjects;
    private readonly IShopRepository          _shops;
    private readonly IStorageService          _storage;
    private readonly ILogger<ProductOgImageService> _logger;

    public ProductOgImageService(
        IProductRepository products,
        IStorageObjectRepository storageObjects,
        IShopRepository shops,
        IStorageService storage,
        ILogger<ProductOgImageService> logger)
    {
        _products       = products;
        _storageObjects = storageObjects;
        _shops          = shops;
        _storage        = storage;
        _logger         = logger;
    }

    public string OgKeyForSlug(string slug) => $"og/product/{slug}.jpg";

    public async Task<string?> GenerateAndStoreAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            var product = await _products.GetProductById(productId);
            if (product is null) return null;

            var price = "$" + product.GetCurrentPrice().ToString("0.##", CultureInfo.InvariantCulture);

            // Фичи = PreviewName активных контент-файлов (уникальные)
            var features = product.ContentFiles
                .Where(f => !f.IsArchived && !string.IsNullOrWhiteSpace(f.PreviewName))
                .Select(f => f.PreviewName)
                .Distinct()
                .ToList();

            // Превью = первое медиа (по сортировке); предпочитаем thumbnail (статичный),
            // иначе само медиа если это картинка (видео без thumbnail пропускаем).
            Stream? previewStream = null;
            var firstMedia = product.MediaProducts.OrderBy(m => m.SortOrder).FirstOrDefault();
            if (firstMedia is not null)
            {
                string? previewKey = null;
                if (firstMedia.ThumbnailId is Guid thumbId)
                    previewKey = (await _storageObjects.GetByIdAsync(thumbId))?.Key;
                if (previewKey is null)
                {
                    var so = await _storageObjects.GetByIdAsync(firstMedia.StorageObjectId);
                    if (so is not null && so.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        previewKey = so.Key;
                }
                if (previewKey is not null)
                    previewStream = await _storage.OpenReadStreamAsync(previewKey);
            }

            // Продавец: имя + лого из декорации магазина
            var shop = await _shops.GetShopByIdAsync(product.OwnerId);
            var sellerName = string.IsNullOrWhiteSpace(shop?.Name) ? "CreoHub" : shop!.Name;
            Stream? logoStream = null;
            if (shop?.LogoId is Guid logoId)
            {
                var logoSo = await _storageObjects.GetByIdAsync(logoId);
                if (logoSo is not null)
                    logoStream = await _storage.OpenReadStreamAsync(logoSo.Key);
            }

            var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Og");

            byte[] bytes;
            using (previewStream)
            using (logoStream)
            {
                bytes = ProductOgRenderer.Render(
                    assetsDir, previewStream, logoStream,
                    new ProductOgData(product.Name, price, features, sellerName));
            }

            var key = OgKeyForSlug(product.Slug);
            using var ms = new MemoryStream(bytes);
            await _storage.UploadFileAsync(ms, key, "image/jpeg");

            _logger.LogInformation("OG image generated for product {Id} → {Key}", productId, key);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OG image generation failed for product {Id}", productId);
            return null;
        }
    }
}

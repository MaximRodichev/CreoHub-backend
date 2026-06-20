namespace CreoHub.Application.Services;

/// <summary>
/// Генерация og:image карточки товара (бренд-шаблон + превью + имя/цена/фичи/продавец)
/// и сохранение в R2. og:image страницы товара ссылается на отдельный публичный эндпоинт.
/// </summary>
public interface IProductOgImageService
{
    /// <summary>Рендерит карточку товара и кладёт PNG в R2. Возвращает R2-ключ или null при ошибке.</summary>
    Task<string?> GenerateAndStoreAsync(int productId, CancellationToken ct = default);

    /// <summary>R2-ключ og-карточки для slug (og/product/{slug}.png).</summary>
    string OgKeyForSlug(string slug);
}

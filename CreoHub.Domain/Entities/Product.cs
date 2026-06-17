using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Product
{
    private readonly List<ProductBundle> _bundleItems = new();
    private readonly List<Price> _prices = new();
    private readonly List<OrderItem> _orderItems = new();
    private readonly List<MediaProduct> _mediaProducts = new();
    private readonly List<ContentFile> _contentFiles = new();
    private readonly List<Tag> _tags = new();

    // PartialPurchaseAlpha удалён — динамический α вычисляется в Application слое
    // через PricingConfig.ComputeAlpha(totalFiles) и передаётся в CalculatePrice.

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; private init; }
    public string Name { get; private set; }
    /// <summary>
    /// URL-идентификатор продукта. Генерируется из Name при создании,
    /// может быть переопределён вручную. Не меняется автоматически при смене Name.
    /// Уникален в рамках всей платформы.
    /// </summary>
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

    public ProductType ProductType { get; private set; } = ProductType.Single;
    public ProductStatus ProductStatus { get; private set; } = ProductStatus.OnModerating;
    public string? BanReason { get; private set; }

    public Shop Owner { get; private init; }
    public Guid OwnerId { get; private init; }

    public IReadOnlyCollection<ProductBundle> BundleItems => _bundleItems.AsReadOnly();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<Price> Prices => _prices.AsReadOnly();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyCollection<MediaProduct> MediaProducts => _mediaProducts.AsReadOnly();
    public IReadOnlyCollection<ContentFile> ContentFiles => _contentFiles.AsReadOnly();

    private Product() {}

    /// <summary>
    /// Карта транслитерации кириллица → латиница для slug.
    /// Должна совпадать с SQL-картой в миграции TransliterateProductSlugs
    /// и JS-зеркалом во фронтенде (lib/translit.js).
    /// </summary>
    private static readonly Dictionary<char, string> Translit = new()
    {
        ['а']="a", ['б']="b", ['в']="v", ['г']="g", ['д']="d", ['е']="e", ['ё']="yo",
        ['ж']="zh", ['з']="z", ['и']="i", ['й']="y", ['к']="k", ['л']="l", ['м']="m",
        ['н']="n", ['о']="o", ['п']="p", ['р']="r", ['с']="s", ['т']="t", ['у']="u",
        ['ф']="f", ['х']="h", ['ц']="c", ['ч']="ch", ['ш']="sh", ['щ']="shch",
        ['ъ']="", ['ы']="y", ['ь']="", ['э']="e", ['ю']="yu", ['я']="ya",
    };

    /// <summary>
    /// Генерирует URL-безопасный slug из произвольного названия.
    /// "Fire Pack: Vol. 3 — Premium" → "fire-pack-vol-3-premium"
    /// "Анимационные материалы" → "animacionnye-materialy"
    /// </summary>
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        var lower = name.ToLowerInvariant();

        var sb = new System.Text.StringBuilder(lower.Length + 8);
        foreach (var ch in lower)
            sb.Append(Translit.TryGetValue(ch, out var lat) ? lat : ch);

        var s = sb.ToString();
        s = Regex.Replace(s, @"[^\w\s-]", " ");   // оставляем буквы, цифры, пробелы, дефисы
        s = Regex.Replace(s, @"\s+", "-");          // пробелы → дефисы
        s = Regex.Replace(s, @"-{2,}", "-");         // двойные дефисы → одиночные
        s = s.Trim('-');

        // Имя целиком из спецсимволов/удаляемых букв → slug не должен быть пустым
        // (unique index): подстраховка случайным суффиксом.
        return s.Length > 0 ? s : $"product-{Guid.NewGuid():N}"[..16];
    }

    public const int MaxNameLength        = 50;
    public const int MaxDescriptionLength = 2000;

    public Product(string name, string description, Guid ownerId, IEnumerable<Tag>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Имя товара не должно превышать {MaxNameLength} символов (сейчас {name.Length}).");
        Name        = name;
        Slug        = GenerateSlug(name);
        if (description == null) throw new ArgumentNullException(nameof(description));
        if (description.Length > MaxDescriptionLength)
            throw new ArgumentException($"Описание не должно превышать {MaxDescriptionLength} символов (сейчас {description.Length}).");
        Description = description;
        OwnerId     = ownerId;

        if (tags != null)
        {
            foreach (var tag in tags)
                AddTag(tag);
        }
    }

    public void AddBundleItems(List<Product> products)
    {
        if (products == null || products.Count == 0)
            throw new ArgumentException("Products cannot be empty.", nameof(products));

        // ── Жёсткое правило: набор не может содержать другой набор ──
        var nestedBundle = products.FirstOrDefault(p => p.ProductType == ProductType.Bundle);
        if (nestedBundle != null)
            throw new InvalidOperationException(
                $"Набор не может содержать другой набор. «{nestedBundle.Name}» уже является набором. " +
                "Добавляйте только одиночные товары (Single).");

        ProductType = ProductType.Bundle;

        foreach (var product in products)
        {
            if (_bundleItems.All(b => b.ProductId != product.Id))
            {
                _bundleItems.Add(new ProductBundle(Id, product.Id));
            }
        }
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (name.Length > MaxNameLength)
            throw new ArgumentException($"Имя товара не должно превышать {MaxNameLength} символов (сейчас {name.Length}).");
        Name = name;
        // Slug НЕ меняется автоматически — URL должен оставаться стабильным
    }

    /// <summary>
    /// Явное переопределение slug (если null/пустой — регенерировать из текущего Name).
    /// Ручной ввод проходит ту же санитизацию, что и автогенерация:
    /// транслит, lowercase, спецсимволы → дефисы.
    /// </summary>
    public void UpdateSlug(string? slug = null)
    {
        Slug = string.IsNullOrWhiteSpace(slug) ? GenerateSlug(Name) : GenerateSlug(slug);
    }

    public void UpdateDescription(string description)
    {
        if (description == null) throw new ArgumentNullException(nameof(description));
        if (description.Length > MaxDescriptionLength)
            throw new ArgumentException($"Описание не должно превышать {MaxDescriptionLength} символов (сейчас {description.Length}).");
        Description = description;
    }

    public void AddPrice(decimal amount)
    {
        _prices.Add(new Price(amount, Id));
    }

    public decimal GetCurrentPrice()
    {
        return _prices
            .OrderByDescending(p => p.Date)
            .FirstOrDefault()
            ?.Value 
            ?? throw new InvalidOperationException($"Product {Id} has no prices.");
    }
    
    public void AddTag(Tag tag)
    {
        if (tag == null)
            throw new ArgumentNullException(nameof(tag));
    
        if (_tags.All(t => t.Id != tag.Id))
            _tags.Add(tag);
    }

    public void RemoveTag(Tag tag)
    {
        if (tag == null)
            throw new ArgumentNullException(nameof(tag));
    
        var existing = _tags.FirstOrDefault(t => t.Id == tag.Id)
                       ?? throw new InvalidOperationException("Tag not found.");
    
        _tags.Remove(existing);
    }
    
    public void RemoveMedia(MediaProduct media)
    {
        if (media == null)
            throw new ArgumentNullException(nameof(media));

        if (!_mediaProducts.Remove(media))
            throw new InvalidOperationException("Media not found.");
    }
    
    public void AddMedia(MediaProduct media)
    {
        if (media == null)
            throw new ArgumentNullException(nameof(media));

        _mediaProducts.Add(media);
    }
    
    public void ReplaceTags(IEnumerable<Tag> newTags)
    {
        if (newTags == null)
            throw new ArgumentNullException(nameof(newTags));

        _tags.Clear();
        foreach (var tag in newTags)
            AddTag(tag);
    }

    /// <summary>
    /// Цена за подмножество файлов по степенной кривой:
    ///   price = basePrice × ratio^α
    /// где ratio = selectedWeight / totalWeight.
    /// α вычисляется в Application слое через PricingConfig.ComputeAlpha(ContentFiles.Count).
    /// При ratio = 1 возвращает полную цену без наценки.
    /// </summary>
    public decimal CalculatePrice(List<ContentFile> selectedFiles, double alpha)
    {
        var totalWeight = _contentFiles.Sum(f => f.PriceWeight);
        if (totalWeight == 0)
            throw new InvalidOperationException("Product has no content files.");

        if (selectedFiles == null || selectedFiles.Count == 0)
            throw new ArgumentException("Files cannot be empty.", nameof(selectedFiles));

        var selectedWeight = selectedFiles.Sum(f => f.PriceWeight);
        var ratio = (double)selectedWeight / totalWeight;

        var price = (double)GetCurrentPrice() * Math.Pow(ratio, alpha);

        return Math.Round((decimal)price, 2);
    }

    public void Activate()
    {
        if (ProductStatus == ProductStatus.Banned)
            throw new InvalidOperationException("Banned products can only be activated by admin.");
        ProductStatus = ProductStatus.Active;
    }

    public void Hide()
    {
        if (ProductStatus == ProductStatus.Banned)
            throw new InvalidOperationException("Banned products cannot be hidden.");
        ProductStatus = ProductStatus.Hidden;
    }

    /// <summary>
    /// Отправить на модерацию. Допускается из Hidden, ModerationFailed.
    /// Также вызывается автоматически при любом редактировании.
    /// </summary>
    public void SendToModeration()
    {
        if (ProductStatus == ProductStatus.Banned)
            throw new InvalidOperationException("Banned products cannot be sent to moderation.");
        if (ProductStatus == ProductStatus.Archived)
            throw new InvalidOperationException("Archived products cannot be sent to moderation.");
        ProductStatus = ProductStatus.OnModerating;
    }

    public void ApproveModeration()
    {
        if (ProductStatus != ProductStatus.OnModerating)
            throw new InvalidOperationException("Only products on moderation can be approved.");
        ProductStatus = ProductStatus.Active;
    }

    public void RejectModeration()
    {
        if (ProductStatus != ProductStatus.OnModerating)
            throw new InvalidOperationException("Only products on moderation can be rejected.");
        ProductStatus = ProductStatus.ModerationFailed;
    }

    /// <summary>Администратор банит товар с указанием причины.</summary>
    public void Ban(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Ban reason is required.", nameof(reason));
        if (ProductStatus == ProductStatus.Archived)
            throw new InvalidOperationException("Archived products cannot be banned.");
        ProductStatus = ProductStatus.Banned;
        BanReason = reason;
    }

    /// <summary>Администратор снимает бан.</summary>
    public void Unban()
    {
        if (ProductStatus != ProductStatus.Banned)
            throw new InvalidOperationException("Only banned products can be unbanned.");
        ProductStatus = ProductStatus.Hidden;
        BanReason = null;
    }

    /// <summary>
    /// Администратор принудительно скрывает товар.
    /// Работает при любом статусе кроме Archived и Banned.
    /// </summary>
    public void AdminHide()
    {
        if (ProductStatus == ProductStatus.Archived)
            throw new InvalidOperationException("Нельзя скрыть архивированный товар.");
        if (ProductStatus == ProductStatus.Banned)
            throw new InvalidOperationException("Нельзя скрыть забаненный товар. Используйте Unban.");
        ProductStatus = ProductStatus.Hidden;
    }

    /// <summary>
    /// Администратор принудительно отправляет товар на модерацию.
    /// Работает при любом статусе кроме Archived и Banned.
    /// </summary>
    public void AdminSendToModeration()
    {
        if (ProductStatus == ProductStatus.Archived)
            throw new InvalidOperationException("Нельзя отправить на модерацию архивированный товар.");
        if (ProductStatus == ProductStatus.Banned)
            throw new InvalidOperationException("Нельзя отправить на модерацию забаненный товар. Снимите бан сначала.");
        ProductStatus = ProductStatus.OnModerating;
    }

    /// <summary>
    /// Архивирует товар: скрывает из каталога, но сохраняет доступ покупателей.
    /// Вызывается вместо удаления если у товара есть покупатели.
    /// </summary>
    public void Archive()
    {
        ProductStatus = ProductStatus.Archived;
    }
    
    public void AddContentFile(ContentFile contentFile)
    {
        if (contentFile == null)
            throw new ArgumentNullException(nameof(contentFile));
        _contentFiles.Add(contentFile);
    }

    public void RemoveContentFile(ContentFile contentFile)
    {
        if (contentFile == null)
            throw new ArgumentNullException(nameof(contentFile));
        _contentFiles.Remove(contentFile);
    }
}

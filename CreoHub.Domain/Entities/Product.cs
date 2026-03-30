using System.ComponentModel.DataAnnotations.Schema;
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

    private const decimal PartialPurchaseMarkup = 0.30m;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; private init; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

    public ProductType ProductType { get; private set; } = ProductType.Single;
    public ProductStatus ProductStatus { get; private set; } = ProductStatus.Active;

    public Shop Owner { get; private init; }
    public Guid OwnerId { get; private init; }

    public IReadOnlyCollection<ProductBundle> BundleItems => _bundleItems.AsReadOnly();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<Price> Prices => _prices.AsReadOnly();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyCollection<MediaProduct> MediaProducts => _mediaProducts.AsReadOnly();
    public IReadOnlyCollection<ContentFile> ContentFiles => _contentFiles.AsReadOnly();

    private Product() {}

    public Product(string name, string description, Guid ownerId, IEnumerable<Tag>? tags = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        OwnerId = ownerId;
        
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
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void UpdateDescription(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
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

    public decimal CalculatePrice(List<ContentFile> selectedFiles)
    {
        if (selectedFiles == null || selectedFiles.Count == 0)
            throw new ArgumentException("Files cannot be empty.", nameof(selectedFiles));

        var totalWeight = _contentFiles.Sum(f => f.PriceWeight);
        if (totalWeight == 0)
            throw new InvalidOperationException("Product has no content files.");

        var selectedWeight = selectedFiles.Sum(f => f.PriceWeight);
        var ratio = (decimal)selectedWeight / totalWeight;
        var basePrice = GetCurrentPrice() * ratio;
        var markup = PartialPurchaseMarkup * (1 - ratio);

        return Math.Round(basePrice * (1 + markup), 2);
    }

    public void Activate()
    {
        if (ProductStatus != ProductStatus.Hidden)
            throw new InvalidOperationException("Only hidden products can be activated.");
        ProductStatus = ProductStatus.Active;
    }

    public void Hide()
    {
        if (ProductStatus != ProductStatus.Active)
            throw new InvalidOperationException("Only active products can be hidden.");
        ProductStatus = ProductStatus.Hidden;
    }

    public void SendToModeration()
    {
        if (ProductStatus != ProductStatus.Active)
            throw new InvalidOperationException("Only active products can be sent to moderation.");
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

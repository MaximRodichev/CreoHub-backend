namespace CreoHub.Domain.Entities;

/// <summary>
/// Возможность делать SuperТовары, которые содержат в себе множество отдельные товаров
/// </summary>
public class ProductBundle
{
    public Product Bundle { get; private init; }
    public int BundleId { get; private init; }
    public Product Product { get; private init; }
    public int ProductId { get; private init; }

    private ProductBundle() {}

    public ProductBundle(int bundleId, int productId)
    {
        if (bundleId == productId)
            throw new ArgumentException("Bundle cannot contain itself.");

        BundleId = bundleId;
        ProductId = productId;
    }
}

namespace CreoHub.Domain.Entities;

/// <summary>
/// Возможность делать SuperТовары, которые содержат в себе множество отдельные товаров
/// </summary>
public class ProductBundle
{
    /// <summary>
    /// Идентификатор якорного продукта
    /// </summary>
    public Product Bundle { get; private set; }
    public int BundleId { get; private set; }
    public Product Product { get; private set; }
    public int ProductId { get; private set; }
}
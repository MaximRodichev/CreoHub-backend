using CreoHub.CRM.Domain.Exceptions;
using CreoHub.Domain.Entities;

public class Price
{
    public DateTime Date { get; private init; } = DateTime.UtcNow;
    public decimal Value { get; private init; }

    public Product Product { get; private init; }
    public int ProductId { get; private init; }

    private Price() {}

    public Price(decimal amount, int productId)
    {
        if (amount <= 0)
            throw new NegativeOrZeroPriceException();

        Value = amount;
        ProductId = productId;
    }

    public Price(decimal amount, Product product)
    {
        if (amount <= 0)
            throw new NegativeOrZeroPriceException();
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        Value = amount;
        Product = product;
        ProductId = product.Id;
    }
}
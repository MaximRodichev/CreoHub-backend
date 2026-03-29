namespace CreoHub.Domain.Entities;

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
            throw new ArgumentException("Price must be greater than zero.", nameof(amount));

        Value = amount;
        ProductId = productId;
    }
}
using CreoHub.CRM.Domain.Exceptions;

namespace CreoHub.Domain.Entities;

public class Price
{
    public DateTime Date { get; set; } =  DateTime.Now;
    public decimal PriceAtMoment { get; set; }
    
    //FK
    public Product Product { get; set; }
    public int ProductId { get; set; }

    public Price()
    {
        
    }

    public Price(decimal amount, Product product)
    {
        if (PriceAtMoment < 0)
        {
            throw new NegativeOrZeroPriceException();
        }
        PriceAtMoment = amount;
        Product = product;
        ProductId = product.Id;
    }
    
    public Price(decimal amount, int productId)
    {
        if (PriceAtMoment < 0)
        {
            throw new NegativeOrZeroPriceException();
        }
        PriceAtMoment = amount;
        ProductId = productId;
    }
}
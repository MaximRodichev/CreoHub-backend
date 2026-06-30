namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderSellDTO
{
    public DateTime BuyDate { get; set; }
    public string CustomerName { get; set; }
    /// <summary>Сумма, уплаченная покупателем за этот товар (PriceAtPurchase из OrderItem).</summary>
    public decimal Amount { get; set; }

    /// <summary>True — куплены не все файлы продукта (частичная покупка). False — куплен весь продукт.</summary>
    public bool IsPartial { get; set; }
    /// <summary>Имена купленных файлов — заполняется только при частичной покупке (IsPartial).</summary>
    public List<string>? PurchasedFiles { get; set; }
}
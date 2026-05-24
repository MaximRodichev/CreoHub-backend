namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderSellDTO
{
    public DateTime BuyDate { get; set; }
    public string CustomerName { get; set; }
    /// <summary>Сумма, уплаченная покупателем за этот товар (PriceAtPurchase из OrderItem).</summary>
    public decimal Amount { get; set; }
}
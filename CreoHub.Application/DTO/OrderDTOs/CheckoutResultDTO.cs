namespace CreoHub.Application.DTO.OrderDTOs;

public class CheckoutResultDTO
{
    public Guid OrderId { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

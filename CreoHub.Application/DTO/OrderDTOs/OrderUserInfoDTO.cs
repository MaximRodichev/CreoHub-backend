using System.Text.Json.Serialization;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderUserInfoDTO
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }
    
    public decimal TotalPrice { get; set; }
    
    public Guid? TransactionId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionStatus? TransactionStatus { get; set; }
    public string? TxHash { get; set; }
    public DateTime? PaidAt { get; set; }
    
    public List<OrderItemDTO> Items { get; set; }
}

public class OrderItemDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal PriceAtPurchase { get; set; }
}
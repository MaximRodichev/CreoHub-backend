using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderFullInfoDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName  { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
}
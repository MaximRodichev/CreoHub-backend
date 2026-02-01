using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderFullInfoDTO
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public List<ProductOrderInfoDTO> ProductItems { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
}
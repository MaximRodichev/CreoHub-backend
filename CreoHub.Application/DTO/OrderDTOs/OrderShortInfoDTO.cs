using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderShortInfoDTO
{
    public string ProductName { get; set; }
    public string CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Price { get; set; }
    public String Status { get; set; }
}
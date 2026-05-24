namespace CreoHub.Application.DTO.OrderDTOs;

public class CreateOrderDevDTO
{
    public Guid       ClientId    { get; set; }
    public List<int>  ProductsIds { get; set; }
    public decimal    Price       { get; set; }
}
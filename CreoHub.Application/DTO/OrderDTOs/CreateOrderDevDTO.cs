namespace CreoHub.Application.DTO.OrderDTOs;

public class CreateOrderDevDTO
{
    public Guid ClientId { get; set; }
    public decimal Price { get; set; }
    public int ProductId { get; set; }
    //TODO: Удалить date когда заполнится бд руками, дата не должна передаваться, а должна генерироваться
    public DateTime PurchaseDate { get; set; }
}
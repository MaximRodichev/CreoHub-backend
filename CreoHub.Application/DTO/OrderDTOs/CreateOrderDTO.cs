namespace CreoHub.Application.DTO.OrderDTOs;

public class CreateOrderDTO
{
    public int ProductId { get; set; }
    //TODO: Удалить date когда заполнится бд руками, дата не должна передаваться, а должна генерироваться
    public DateTime Date { get; set; }
}
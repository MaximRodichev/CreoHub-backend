namespace CreoHub.Application.DTO.OrderDTOs;

//TODO: Дев версия
public class CreateOrderDTO
{
    public List<int> ProductsIds { get; set; }
    //TODO: Удалить date когда заполнится бд руками, дата не должна передаваться, а должна генерироваться
    public DateTime Date { get; set; }
}
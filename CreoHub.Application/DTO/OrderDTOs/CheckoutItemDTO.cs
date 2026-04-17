namespace CreoHub.Application.DTO.OrderDTOs;

/// <summary>
/// Один товар в корзине с выбранными файлами.
/// FileIds пустой = полная покупка всего продукта.
/// </summary>
public class CheckoutItemDTO
{
    public int ProductId { get; set; }
    public List<Guid> FileIds { get; set; } = new();
}

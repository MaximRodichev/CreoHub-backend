namespace CreoHub.Application.DTO.CartDTOs;

/// <summary>
/// Опциональное тело запроса toggle-item.
/// FileIds — конкретные файлы, выбранные пользователем на странице товара.
/// Пустой список или отсутствие тела = все файлы продукта.
/// </summary>
public class ToggleCartItemBodyDTO
{
    public List<Guid>? FileIds { get; set; }
}

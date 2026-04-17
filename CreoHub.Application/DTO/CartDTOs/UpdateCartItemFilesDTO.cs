namespace CreoHub.Application.DTO.CartDTOs;

public class UpdateCartItemFilesDTO
{
    /// <summary>Список выбранных файлов. Пустой = вся корзина без частичного выбора.</summary>
    public List<Guid> FileIds { get; set; } = new();
}

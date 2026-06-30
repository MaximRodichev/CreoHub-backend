using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderShortInfoDTO
{
    public Guid Id { get; set; }
    public List<string> ProductNames { get; set; }
    public string CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Price { get; set; }
    public String Status { get; set; }

    /// <summary>Позиции этого магазина, купленные частично (не все файлы). Пусто = все покупки полные.</summary>
    public List<OrderLinePartialDTO> PartialItems { get; set; } = new();
}

/// <summary>Частично купленная позиция заказа — для показа «какие именно файлы».</summary>
public class OrderLinePartialDTO
{
    public string ProductName { get; set; } = string.Empty;
    public List<string> FileNames { get; set; } = new();
    public int BoughtCount { get; set; }
    public int TotalCount { get; set; }
}
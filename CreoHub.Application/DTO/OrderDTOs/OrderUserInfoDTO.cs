using System.Text.Json.Serialization;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.OrderDTOs;

public class OrderUserInfoDTO
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    /// <summary>Сумма позиций без скидок.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Сумма к оплате (после скидок).</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Персональная скидка (lifetime), доля [0,1].</summary>
    public decimal PersonalDiscountPercent { get; set; }

    /// <summary>Скидка за объём корзины, доля [0,1].</summary>
    public decimal CartDiscountPercent { get; set; }

    /// <summary>Итоговая скидка, доля [0,1].</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Абсолютная экономия.</summary>
    public decimal DiscountAmount { get; set; }

    public Guid? TransactionId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TransactionStatus? TransactionStatus { get; set; }
    public string? TxHash { get; set; }
    public DateTime? PaidAt { get; set; }

    public List<OrderItemDTO> Items { get; set; } = new();
}

public class OrderItemDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSlug { get; set; }
    public decimal PriceAtPurchase { get; set; }

    /// <summary>true для бандл-продуктов</summary>
    public bool IsBundle { get; set; }

    /// <summary>
    /// Купленные контент-файлы по этому товару (только для Single-продуктов).
    /// Берётся из ContentAccess WHERE OrderId = order AND ContentFile.ProductId = product.
    /// </summary>
    public List<PurchasedFileDTO> Files { get; set; } = new();

    /// <summary>
    /// Для бандлов — входящие продукты с их файлами.
    /// </summary>
    public List<BundleOrderItemDTO>? BundleItems { get; set; }
}

public class BundleOrderItemDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSlug { get; set; }
    public List<PurchasedFileDTO> Files { get; set; } = new();
}

public class PurchasedFileDTO
{
    public Guid ContentFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
}

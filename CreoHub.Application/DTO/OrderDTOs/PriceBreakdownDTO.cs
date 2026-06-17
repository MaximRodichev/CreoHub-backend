namespace CreoHub.Application.DTO.OrderDTOs;

public class PriceBreakdownDTO
{
    /// <summary>Сумма до скидки.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Итого к оплате (после скидки).</summary>
    public decimal Total { get; set; }

    /// <summary>Суммарный процент скидки (0–21).</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Абсолютная сумма скидки.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Детализация скидок.</summary>
    public DiscountBreakdownDTO DiscountBreakdown { get; set; } = new();

    public List<PriceLineDTO> Lines { get; set; } = new();
}

public class DiscountBreakdownDTO
{
    /// <summary>Скидка новому покупателю на первый заказ, % (0 или 20).</summary>
    public decimal WelcomeDiscountPercent { get; set; }

    /// <summary>Скидка за накопленный спенд, % (0–12).</summary>
    public decimal LifetimeDiscountPercent { get; set; }

    /// <summary>Скидка за объём корзины, % (0–9).</summary>
    public decimal CartVolumeDiscountPercent { get; set; }
}

public class PriceLineDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SelectedFilesCount { get; set; }
    public int TotalFilesCount { get; set; }
    public bool IsPartialPurchase { get; set; }
    /// <summary>Цена к оплате (после скидки частичного владения бандлом).</summary>
    public decimal Price { get; set; }
    /// <summary>
    /// Полная цена до скидки частичного владения.
    /// Равна Price если скидки нет — фронт показывает строку "Скидка за набор" только если OriginalPrice > Price.
    /// </summary>
    public decimal OriginalPrice { get; set; }
}

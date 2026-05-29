namespace CreoHub.Domain.Services;

/// <summary>
/// Вспомогательный статический класс для расчёта скидок (Блок F).
/// Все методы возвращают долю [0, 1] — не процент.
/// </summary>
public static class DiscountCalculator
{
    /// <summary>
    /// F2a. Скидка за объём корзины (устаревшая, по сумме).
    /// Оставлена для обратной совместимости.
    /// </summary>
    public static decimal GetCartVolumeDiscount(decimal cartTotal) => cartTotal switch
    {
        >= 200m => 0.09m,
        >= 100m => 0.06m,
        >= 50m  => 0.03m,
        _       => 0m,
    };

    /// <summary>
    /// F2b. Скидка за количество товаров в корзине.
    /// 3+ → 3%, 5+ → 6%, 8+ → 9%, 12+ → 12%.
    /// Это основное правило для всех checkout-путей.
    /// </summary>
    public static decimal GetCartCountDiscount(int itemCount) => itemCount switch
    {
        >= 12 => 0.12m,
        >= 8  => 0.09m,
        >= 5  => 0.06m,
        >= 3  => 0.03m,
        _     => 0m,
    };

    /// <summary>
    /// Применяет наибольшую из двух скидок (MAX, не сумма).
    /// Логика: пользователь получает лучшую скидку — либо за лояльность, либо за объём.
    /// </summary>
    public static decimal GetTotalDiscount(decimal lifetimeDiscount, decimal cartVolumeDiscount)
        => Math.Max(lifetimeDiscount, cartVolumeDiscount);

    /// <summary>
    /// Рассчитывает сумму к оплате с учётом обеих скидок.
    /// Автор всегда получает от полной (rawTotal) суммы.
    /// </summary>
    public static decimal ApplyDiscount(decimal rawTotal, decimal totalDiscount)
        => rawTotal * (1m - totalDiscount);
}

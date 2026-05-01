namespace CreoHub.Domain.Services;

/// <summary>
/// Вспомогательный статический класс для расчёта скидок (Блок F).
/// Все методы возвращают долю [0, 1] — не процент.
/// </summary>
public static class DiscountCalculator
{
    /// <summary>
    /// F2. Скидка за объём корзины.
    /// $50–$99 → 3%, $100–$199 → 6%, $200+ → 9%.
    /// </summary>
    public static decimal GetCartVolumeDiscount(decimal cartTotal) => cartTotal switch
    {
        >= 200m => 0.09m,
        >= 100m => 0.06m,
        >= 50m  => 0.03m,
        _       => 0m,
    };

    /// <summary>
    /// Суммирует lifetime-скидку и cart-volume-скидку.
    /// Возвращает итоговую долю скидки (не более 1).
    /// </summary>
    public static decimal GetTotalDiscount(decimal lifetimeDiscount, decimal cartVolumeDiscount)
    {
        var total = lifetimeDiscount + cartVolumeDiscount;
        return total > 1m ? 1m : total;
    }

    /// <summary>
    /// Рассчитывает сумму к оплате с учётом обеих скидок.
    /// Автор всегда получает от полной (rawTotal) суммы.
    /// </summary>
    public static decimal ApplyDiscount(decimal rawTotal, decimal totalDiscount)
        => rawTotal * (1m - totalDiscount);
}

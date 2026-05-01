namespace CreoHub.Domain.Services;

/// <summary>
/// Вычисляет цену набора (Bundle) с учётом уже купленных файлов дочерних продуктов.
///
/// Алгоритм:
///   1. bundleDiscPct = (sumChildPrices - bundlePrice) / sumChildPrices
///   2. Для каждого дочернего продукта i:
///        effectivePrice_i = childBasePrice_i × (nonOwnedWeight_i / totalWeight_i)^α
///        ownedValue_i     = childBasePrice_i × (ownedWeight_i   / totalWeight_i)^α
///   3. Порог 50%: if ΣownedValue_i / ΣchildBasePrice_i > 0.5 → покупка отклоняется
///   4. finalPrice = ΣeffectivePrice_i × (1 − bundleDiscPct)
///
/// Использует тот же α=0.5, что и Product.CalculatePrice (степенная кривая).
/// </summary>
public static class BundleCalculator
{
    /// <summary>Тот же α, что и в Product.PartialPurchaseAlpha.</summary>
    private const double Alpha = 0.5;

    /// <summary>
    /// Порог: если пользователь уже владеет долей стоимости набора, превышающей это значение,
    /// покупать набор не имеет смысла (скидка на набор работала бы против платформы).
    /// </summary>
    public const double OwnedValueThreshold = 0.5;

    /// <summary>
    /// Вычисляет % скидки набора из хранимых цен.
    /// Возвращает дробь [0, 1], например 0.20 для 20%.
    /// </summary>
    public static decimal GetBundleDiscountFraction(decimal bundlePrice, decimal sumChildPrices)
    {
        if (sumChildPrices <= 0m) return 0m;
        var disc = (sumChildPrices - bundlePrice) / sumChildPrices;
        return disc < 0m ? 0m : disc;
    }

    /// <summary>
    /// Цена НЕ-купленной части файлов одного дочернего продукта по экспоненциальной кривой.
    /// Возвращает 0 если все файлы уже куплены или вес нулевой.
    /// </summary>
    public static decimal EffectiveNonOwnedPrice(decimal basePrice, int totalWeight, int ownedWeight)
    {
        if (totalWeight <= 0 || basePrice <= 0m) return 0m;
        var nonOwnedWeight = totalWeight - ownedWeight;
        if (nonOwnedWeight <= 0) return 0m;
        var ratio = (double)nonOwnedWeight / totalWeight;
        return (decimal)((double)basePrice * Math.Pow(ratio, 1.0 - Alpha));
    }

    /// <summary>
    /// «Ценность» уже купленных файлов одного дочернего продукта по той же кривой.
    /// Используется для проверки порога 50%.
    /// </summary>
    public static decimal EffectiveOwnedValue(decimal basePrice, int totalWeight, int ownedWeight)
    {
        if (totalWeight <= 0 || ownedWeight <= 0 || basePrice <= 0m) return 0m;
        var ratio = (double)ownedWeight / totalWeight;
        return (decimal)((double)basePrice * Math.Pow(ratio, 1.0 - Alpha));
    }

    /// <summary>
    /// Итоговый результат расчёта частичной покупки набора.
    /// </summary>
    public record BundleAdjustmentResult(
        /// <summary>Финальная цена с учётом уже купленного + скидки набора.</summary>
        decimal FinalPrice,
        /// <summary>Сумма effectivePrice_i до применения скидки набора.</summary>
        decimal AdjustedSubtotal,
        /// <summary>% скидки набора (0–1).</summary>
        decimal BundleDiscountFraction,
        /// <summary>Доля стоимости, которой пользователь уже владеет (0–1).</summary>
        decimal OwnedFraction,
        /// <summary>true — пользователь уже купил слишком много для выгодной покупки набора.</summary>
        bool ExceedsThreshold
    );

    /// <summary>
    /// Рассчитывает итоговую цену покупки набора с учётом владения.
    /// </summary>
    /// <param name="bundlePrice">Хранимая цена набора.</param>
    /// <param name="children">
    ///   Список (basePrice, totalWeight, ownedWeight) для каждого дочернего продукта.
    ///   totalWeight = сумма PriceWeight всех ContentFile продукта;
    ///   ownedWeight = сумма PriceWeight файлов, которыми пользователь уже владеет.
    /// </param>
    public static BundleAdjustmentResult Calculate(
        decimal bundlePrice,
        IReadOnlyList<(decimal BasePrice, int TotalWeight, int OwnedWeight)> children)
    {
        if (children.Count == 0)
        {
            // Бандл без дочерних продуктов — продаём по полной цене, порог не применяется
            return new BundleAdjustmentResult(
                FinalPrice:              bundlePrice,
                AdjustedSubtotal:        bundlePrice,
                BundleDiscountFraction:  0m,
                OwnedFraction:           0m,
                ExceedsThreshold:        false);
        }

        var sumChildPrices   = children.Sum(c => c.BasePrice);
        var discFraction     = GetBundleDiscountFraction(bundlePrice, sumChildPrices);

        var totalOwnedValue   = 0m;
        var adjustedSubtotal  = 0m;

        foreach (var (basePrice, totalWeight, ownedWeight) in children)
        {
            totalOwnedValue  += EffectiveOwnedValue(basePrice, totalWeight, ownedWeight);
            adjustedSubtotal += EffectiveNonOwnedPrice(basePrice, totalWeight, ownedWeight);
        }

        var ownedFraction    = sumChildPrices > 0m ? totalOwnedValue / sumChildPrices : 0m;
        var exceedsThreshold = (double)ownedFraction > OwnedValueThreshold;

        var finalPrice = adjustedSubtotal * (1m - discFraction);

        return new BundleAdjustmentResult(
            FinalPrice:              finalPrice,
            AdjustedSubtotal:        adjustedSubtotal,
            BundleDiscountFraction:  discFraction,
            OwnedFraction:           ownedFraction,
            ExceedsThreshold:        exceedsThreshold);
    }
}

namespace CreoHub.Application.Pricing;

/// <summary>
/// Параметры кривой частичной покупки.
/// C(N) = MIN(MaxOvershoot, MinOvershoot + (MaxOvershoot - MinOvershoot) × ln(N) / ln(CapN))
/// α(N) = 1 − ln(C(N)) / ln(N)
/// price = basePrice × ratio^α,  где ratio = selectedWeight / totalWeight
///
/// При равных весах и N=CapN: суммарная соло-цена = basePrice × MaxOvershoot.
/// </summary>
public class PricingConfig
{
    public const string SectionName = "Pricing";

    /// <summary>Начальный overshoot-множитель (при малом N). Например 1.2 = 120%.</summary>
    public double MinOvershoot { get; set; } = 1.2;

    /// <summary>Максимальный overshoot-множитель. Например 2.0 = 200%.</summary>
    public double MaxOvershoot { get; set; } = 2.0;

    /// <summary>Количество файлов при котором достигается MaxOvershoot.</summary>
    public int CapN { get; set; } = 30;

    /// <summary>
    /// Вычисляет динамический α для продукта с totalFiles файлами.
    /// </summary>
    public double ComputeAlpha(int totalFiles)
    {
        if (totalFiles <= 1) return 1.0;
        var c = Math.Min(MaxOvershoot,
            MinOvershoot + (MaxOvershoot - MinOvershoot) * Math.Log(totalFiles) / Math.Log(CapN));
        return 1.0 - Math.Log(c) / Math.Log(totalFiles);
    }
}

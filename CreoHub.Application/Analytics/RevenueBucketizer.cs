namespace CreoHub.Application.Analytics;

/// <summary>
/// Раскладывает выручку по непрерывной временной шкале с zero-fill.
/// Шаг оси выбирается по длине периода: ≤45 дней — по дням, ≤120 — по неделям
/// (с понедельника), иначе — по месяцам. Ключи бакетов — ISO-дата начала "yyyy-MM-dd".
/// Чистая логика (без EF) — отсюда же тестируется.
/// </summary>
public static class RevenueBucketizer
{
    public static (Dictionary<string, decimal> Buckets, string Granularity) Build(
        IEnumerable<(DateTime Date, decimal Price)> orders, DateTime from, DateTime to)
    {
        if (to < from) to = from;
        var spanDays = (to.Date - from.Date).TotalDays;
        var gran = spanDays <= 45 ? "day" : spanDays <= 120 ? "week" : "month";

        DateTime Start(DateTime d) => gran switch
        {
            "day"  => d.Date,
            "week" => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7)), // начало недели — понедельник
            _      => new DateTime(d.Year, d.Month, 1),
        };
        DateTime Next(DateTime b) => gran switch
        {
            "day"  => b.AddDays(1),
            "week" => b.AddDays(7),
            _      => b.AddMonths(1),
        };

        // Zero-fill: создаём все бакеты в диапазоне.
        var buckets = new SortedDictionary<DateTime, decimal>();
        for (var b = Start(from); b <= to; b = Next(b))
            buckets[b] = 0m;
        var lastBucket = Start(to);
        if (!buckets.ContainsKey(lastBucket)) buckets[lastBucket] = 0m;

        foreach (var o in orders)
        {
            var b = Start(o.Date);
            buckets.TryGetValue(b, out var cur);
            buckets[b] = cur + o.Price;
        }

        var dict = buckets.ToDictionary(kv => kv.Key.ToString("yyyy-MM-dd"), kv => kv.Value);
        return (dict, gran);
    }
}

using CreoHub.Application.Analytics;

namespace CreoHub.Tests.AnalyticsTests;

/// <summary>Гранулярность по длине периода + zero-fill + суммирование выручки по бакетам.</summary>
public class RevenueBucketizerTests
{
    [Fact]
    public void ShortRange_Daily_ZeroFillsGaps_AndSums()
    {
        var orders = new[]
        {
            (new DateTime(2026, 6, 1, 10, 0, 0), 10m),
            (new DateTime(2026, 6, 1, 15, 0, 0), 5m),
            (new DateTime(2026, 6, 3), 20m),
        };

        var (buckets, gran) = RevenueBucketizer.Build(orders, new DateTime(2026, 6, 1), new DateTime(2026, 6, 5));

        Assert.Equal("day", gran);
        Assert.Equal(5, buckets.Count);              // 1..5 июня — все дни присутствуют
        Assert.Equal(15m, buckets["2026-06-01"]);    // 10 + 5 в один день
        Assert.Equal(0m,  buckets["2026-06-02"]);    // пусто, но не выпало
        Assert.Equal(20m, buckets["2026-06-03"]);
        Assert.Equal(0m,  buckets["2026-06-05"]);
    }

    [Fact]
    public void MidRange_Weekly()
    {
        var orders = new[] { (new DateTime(2026, 1, 7), 50m) };

        var (buckets, gran) = RevenueBucketizer.Build(orders, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        Assert.Equal("week", gran);                  // 89 дней → недели
        Assert.Equal(50m, buckets.Values.Sum());
        Assert.All(buckets.Keys, k => Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", k));
    }

    [Fact]
    public void LongRange_Monthly_BucketsByFirstOfMonth()
    {
        var orders = new[]
        {
            (new DateTime(2026, 1, 15), 100m),
            (new DateTime(2026, 3, 20), 30m),
        };

        var (buckets, gran) = RevenueBucketizer.Build(orders, new DateTime(2026, 1, 1), new DateTime(2026, 6, 30));

        Assert.Equal("month", gran);                 // 180 дней → месяцы
        Assert.Equal(6, buckets.Count);              // янв..июн
        Assert.Equal(100m, buckets["2026-01-01"]);
        Assert.Equal(0m,   buckets["2026-02-01"]);
        Assert.Equal(30m,  buckets["2026-03-01"]);
    }

    [Fact]
    public void EmptyOrders_StillZeroFills()
    {
        var (buckets, gran) = RevenueBucketizer.Build(
            Array.Empty<(DateTime, decimal)>(), new DateTime(2026, 6, 1), new DateTime(2026, 6, 3));

        Assert.Equal("day", gran);
        Assert.Equal(3, buckets.Count);
        Assert.All(buckets.Values, v => Assert.Equal(0m, v));
    }
}

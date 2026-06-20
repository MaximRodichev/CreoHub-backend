using CreoHub.Infrastructure.Persistence.Services;

namespace CreoHub.Tests.OgTests;

/// <summary>
/// Локальный прогон рендера og-карточки товара: пишет JPEG на диск для визуальной сверки.
/// Использует локальные сэмпл-файлы (не CI-тест).
/// </summary>
public class ProductOgRendererTests
{
    private const string AssetsDir = @"E:\Works\Development\creohub\CreoHub-backend\CreoHub.Infrastructure\Assets\Og";
    private const string PreviewPath = @"C:\Users\vymas\Downloads\coinvolcano_800x600_1_11zon.png";
    private const string LogoPath = @"D:\PinterestFlow\og\_tmp\GamblingAssets2.png";
    private const string OutPath = @"D:\PinterestFlow\og\backend_test.jpg";

    [Fact]
    public void RendersProductCard()
    {
        if (!File.Exists(AssetsDir + @"\template.png")) return; // skip if assets not present

        using var prev = File.Exists(PreviewPath) ? File.OpenRead(PreviewPath) : null;
        using var logo = File.Exists(LogoPath) ? File.OpenRead(LogoPath) : null;

        var data = new ProductOgData(
            "Coin Volcano - Материалы слота для дизайнера",
            "$12",
            new[] { "Статика", "Звуки", "Анимации", "Бигвины", "Символы анимационные" },
            "GamblElements");

        var bytes = ProductOgRenderer.Render(AssetsDir, prev, logo, data);

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath)!);
        File.WriteAllBytes(OutPath, bytes);

        Assert.True(bytes.Length > 5000);
    }
}

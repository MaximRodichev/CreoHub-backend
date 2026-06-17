using CreoHub.Domain.Entities;
using Xunit.Abstractions;

namespace CreoHub.Tests.ProductTests;

/// <summary>
/// Тесты генерации URL-слага товара (Product.GenerateSlug / UpdateSlug):
/// транслитерация кириллицы, санитизация спецсимволов, стабильность URL.
/// Карта транслита обязана совпадать с SQL-миграцией TransliterateProductSlugs
/// и JS-зеркалом фронтенда (lib/translit.js).
/// </summary>
public class ProductSlugTests
{
    private readonly ITestOutputHelper _output;

    private static readonly Guid OwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ProductSlugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ── GenerateSlug: транслит + санитизация ─────────────────────────────────

    [Theory]
    [InlineData("Fire Pack: Vol. 3 — Premium",          "fire-pack-vol-3-premium")]
    [InlineData("Анимационные материалы",                "animacionnye-materialy")]
    [InlineData("Joker Stoker Анимационные материалы",   "joker-stoker-animacionnye-materialy")]
    [InlineData("Жёлтый щит",                            "zhyoltyy-shchit")]
    [InlineData("Объём и мощь",                          "obyom-i-moshch")]
    [InlineData("Чаша Шамана",                           "chasha-shamana")]
    [InlineData("UI Kit 2.0",                            "ui-kit-2-0")]
    public void GenerateSlug_TransliteratesAndSanitizes(string name, string expected)
    {
        var slug = Product.GenerateSlug(name);

        _output.WriteLine($"\"{name}\" → \"{slug}\"");
        Assert.Equal(expected, slug);
    }

    [Fact]
    public void GenerateSlug_OnlySpecialChars_FallsBackToProductPrefix()
    {
        // Имя целиком из символов, которые вычищаются — slug не должен быть пустым
        // (на колонке unique index, два пустых слага = конфликт).
        var slug = Product.GenerateSlug("!!!");

        _output.WriteLine($"\"!!!\" → \"{slug}\"");
        Assert.StartsWith("product-", slug);
        Assert.True(slug.Length > "product-".Length);
    }

    [Fact]
    public void GenerateSlug_Whitespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Product.GenerateSlug("   "));
    }

    // ── Конструктор / UpdateName / UpdateSlug ────────────────────────────────

    [Fact]
    public void Constructor_GeneratesSlugFromName()
    {
        var product = new Product("Огненные кнопки", "desc", OwnerId);

        Assert.Equal("ognennye-knopki", product.Slug);
    }

    [Fact]
    public void UpdateName_DoesNotChangeSlug()
    {
        // URL должен оставаться стабильным при переименовании —
        // уже разосланные ссылки продолжают работать.
        var product = new Product("Старое имя", "desc", OwnerId);
        var slugBefore = product.Slug;

        product.UpdateName("Новое имя");

        Assert.Equal(slugBefore, product.Slug);
    }

    [Fact]
    public void UpdateSlug_ManualValue_IsSanitized()
    {
        // Ручной ввод проходит ту же санитизацию, что и автогенерация.
        var product = new Product("Test", "desc", OwnerId);

        product.UpdateSlug("Мой Крутой URL!");

        _output.WriteLine($"Manual slug → \"{product.Slug}\"");
        Assert.Equal("moy-krutoy-url", product.Slug);
    }

    [Fact]
    public void UpdateSlug_Null_RegeneratesFromCurrentName()
    {
        var product = new Product("Зимний пак", "desc", OwnerId);
        product.UpdateSlug("custom-url");

        product.UpdateSlug(null);

        Assert.Equal("zimniy-pak", product.Slug);
    }

    [Fact]
    public void UpdateSlug_Whitespace_RegeneratesFromCurrentName()
    {
        var product = new Product("Зимний пак", "desc", OwnerId);
        product.UpdateSlug("custom-url");

        product.UpdateSlug("   ");

        Assert.Equal("zimniy-pak", product.Slug);
    }
}

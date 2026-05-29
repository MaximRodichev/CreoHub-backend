using System.Net;
using CreoHub.IntegrationTests.Infrastructure;

namespace CreoHub.IntegrationTests;

public sealed class PricingCatalogTests : IntegrationTestBase
{
    public PricingCatalogTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Catalog_exposes_price_without_discount_for_multi_file_single_product()
    {
        var (_, shop) = await DatabaseSeeder.CreateShopOwnerAsync(Fixture.Services);
        var product = await DatabaseSeeder.CreateProductAsync(
            Fixture.Services,
            shop.Id,
            name: "Multi File Product",
            price: 100m,
            contentWeights: [10, 10, 10, 10]);

        using var client = Fixture.CreateClient();

        var response = await client.GetAsync("/api/product/get-products?page=0&pageSize=20");
        using var json = await JsonAssert.ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var item = json.RootElement
            .RequiredProperty("data")
            .RequiredProperty("products")
            .EnumerateArray()
            .Single(p => p.RequiredProperty("id").GetInt32() == product.Id);

        var price = item.RequiredProperty("price").GetDecimal();
        var priceWithoutDiscount = item.RequiredProperty("priceWithoutDiscount").GetDecimal();

        Assert.Equal(100m, price);
        Assert.True(priceWithoutDiscount > price);
    }
}

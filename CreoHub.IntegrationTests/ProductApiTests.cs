using System.Net;
using System.Net.Http.Json;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Infrastructure.Persistence;
using CreoHub.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.IntegrationTests;

public sealed class ProductApiTests : IntegrationTestBase
{
    public ProductApiTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Create_product_for_seeded_shop_owner_persists_product_and_price()
    {
        var (user, _) = await DatabaseSeeder.CreateShopOwnerAsync(Fixture.Services);
        using var client = Fixture.CreateClient(user.Id);

        var response = await client.PostAsJsonAsync("/api/product/create", new CreateProductDTO
        {
            Name = "HTTP Created Product",
            Description = "Created through the real API pipeline.",
            Tags = [],
            Price = 42.5m,
        });
        using var json = await JsonAssert.ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.RootElement.RequiredProperty("status").GetInt32());

        await using var scope = Fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var productId = json.RootElement.RequiredProperty("data").GetInt32();
        var product = await db.Products
            .Include(p => p.Prices)
            .SingleOrDefaultAsync(p => p.Id == productId);

        Assert.NotNull(product);
        Assert.Equal("HTTP Created Product", product!.Name);
        Assert.Equal(42.5m, product.GetCurrentPrice());
    }

    [Fact]
    public async Task Catalog_returns_seeded_active_product_with_presigned_preview()
    {
        var (_, shop) = await DatabaseSeeder.CreateShopOwnerAsync(Fixture.Services);
        var product = await DatabaseSeeder.CreateProductAsync(Fixture.Services, shop.Id, "Catalog Product", 25m, [10, 10]);
        using var client = Fixture.CreateClient();

        var response = await client.GetAsync("/api/product/get-products?page=0&pageSize=20");
        using var json = await JsonAssert.ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.RootElement.RequiredProperty("status").GetInt32());

        var products = json.RootElement
            .RequiredProperty("data")
            .RequiredProperty("products")
            .EnumerateArray()
            .ToList();

        var item = Assert.Single(products, p => p.RequiredProperty("id").GetInt32() == product.Id);
        Assert.Equal("Catalog Product", item.RequiredProperty("name").GetString());
        Assert.StartsWith("https://storage.integration.test/", item.RequiredProperty("previewKey").GetString());
    }
}

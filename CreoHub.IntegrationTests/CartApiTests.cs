using System.Net;
using System.Net.Http.Json;
using CreoHub.Application.DTO.CartDTOs;
using CreoHub.IntegrationTests.Infrastructure;

namespace CreoHub.IntegrationTests;

public sealed class CartApiTests : IntegrationTestBase
{
    public CartApiTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Toggle_item_adds_product_to_user_cart_with_all_content_files_selected()
    {
        var buyer = await DatabaseSeeder.CreateUserAsync(Fixture.Services, "Buyer", "buyer@example.test");
        var (_, shop) = await DatabaseSeeder.CreateShopOwnerAsync(Fixture.Services);
        var product = await DatabaseSeeder.CreateProductAsync(
            Fixture.Services,
            shop.Id,
            name: "Cart Product",
            price: 30m,
            contentWeights: [3, 7]);

        using var client = Fixture.CreateClient(buyer.Id);

        var toggleResponse = await client.PostAsync($"/api/cart/toggle-item?productId={product.Id}", null);
        using var toggleJson = await JsonAssert.ReadJsonAsync(toggleResponse);

        Assert.Equal(HttpStatusCode.OK, toggleResponse.StatusCode);
        Assert.Equal(0, toggleJson.RootElement.RequiredProperty("status").GetInt32());

        var cartResponse = await client.GetAsync("/api/cart/my-cart");
        using var cartJson = await JsonAssert.ReadJsonAsync(cartResponse);

        Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);
        Assert.Equal(0, cartJson.RootElement.RequiredProperty("status").GetInt32());

        var item = Assert.Single(cartJson.RootElement.RequiredProperty("data").EnumerateArray());
        Assert.Equal(product.Id, item.RequiredProperty("productId").GetInt32());
        Assert.Equal(2, item.RequiredProperty("contentFileInfos").GetArrayLength());
        Assert.Equal(2, item.RequiredProperty("selectedContentItems").GetArrayLength());
    }

    [Fact]
    public async Task Update_cart_item_files_replaces_selected_content_files()
    {
        var buyer = await DatabaseSeeder.CreateUserAsync(Fixture.Services, "Buyer", "buyer@example.test");
        var (_, shop) = await DatabaseSeeder.CreateShopOwnerAsync(Fixture.Services);
        var product = await DatabaseSeeder.CreateProductAsync(
            Fixture.Services,
            shop.Id,
            name: "Partial Cart Product",
            price: 40m,
            contentWeights: [5, 5]);

        using var client = Fixture.CreateClient(buyer.Id);
        await client.PostAsync($"/api/cart/toggle-item?productId={product.Id}", null);

        var cartResponse = await client.GetAsync("/api/cart/my-cart");
        using var cartJson = await JsonAssert.ReadJsonAsync(cartResponse);
        var item = Assert.Single(cartJson.RootElement.RequiredProperty("data").EnumerateArray());
        var cartItemId = item.RequiredProperty("cartItemId").GetGuid();
        var firstFileId = item
            .RequiredProperty("contentFileInfos")
            .EnumerateArray()
            .First()
            .RequiredProperty("id")
            .GetGuid();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/cart/item/{cartItemId}/files",
            new UpdateCartItemFilesDTO { FileIds = [firstFileId] });
        using var updateJson = await JsonAssert.ReadJsonAsync(updateResponse);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(0, updateJson.RootElement.RequiredProperty("status").GetInt32());

        var updatedCartResponse = await client.GetAsync("/api/cart/my-cart");
        using var updatedCartJson = await JsonAssert.ReadJsonAsync(updatedCartResponse);
        var updatedItem = Assert.Single(updatedCartJson.RootElement.RequiredProperty("data").EnumerateArray());

        var selected = Assert.Single(updatedItem.RequiredProperty("selectedContentItems").EnumerateArray());
        Assert.Equal(firstFileId, selected.GetGuid());
    }
}

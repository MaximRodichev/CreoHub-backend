using CreoHub.Domain.Entities;
using CreoHub.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.IntegrationTests.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task<User> CreateUserAsync(
        IServiceProvider services,
        string name = "Integration User",
        string? email = "integration.user@example.test")
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = User.Create(name, email);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    public static async Task<(User User, Shop Shop)> CreateShopOwnerAsync(
        IServiceProvider services,
        string userName = "Integration Seller",
        string shopName = "Integration Shop")
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = User.Create(userName, $"{shopName.Replace(" ", ".").ToLowerInvariant()}@example.test");
        var shop = new Shop(shopName, "Shop created by integration test seed.", user.Id);
        user.AssignShop(shop);

        db.Users.Add(user);
        db.Shops.Add(shop);
        await db.SaveChangesAsync();

        return (user, shop);
    }

    public static async Task<Product> CreateProductAsync(
        IServiceProvider services,
        Guid shopId,
        string name = "Integration Product",
        decimal price = 25m,
        IReadOnlyList<int>? contentWeights = null)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product(name, "Product created by integration test seed.", shopId);
        product.AddPrice(price);

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var media = new StorageObject(
            key: $"media/{product.Id}/preview.png",
            fileName: "preview.png",
            fileSize: 1024,
            mimeType: "image/png",
            ownerId: shopId);

        db.StorageObjects.Add(media);
        await db.SaveChangesAsync();

        db.MediaProducts.Add(new MediaProduct(product.Id, media.Id, sortOrder: 0));

        foreach (var (weight, index) in (contentWeights ?? [10]).Select((weight, index) => (weight, index)))
        {
            var storage = new StorageObject(
                key: $"content/{product.Id}/file-{index}.zip",
                fileName: $"file-{index}.zip",
                fileSize: 2048,
                mimeType: "application/zip",
                ownerId: shopId);

            db.StorageObjects.Add(storage);
            await db.SaveChangesAsync();

            db.ContentFiles.Add(new ContentFile(
                priceWeight: weight,
                previewName: $"File {index}",
                storageObjectId: storage.Id,
                productId: product.Id));
        }

        await db.SaveChangesAsync();

        return product;
    }
}

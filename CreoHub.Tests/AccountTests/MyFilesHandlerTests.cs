using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Account;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.AccountTests;

public class MyFilesHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly IContentAccessRepository _contentAccessRepo;
    private readonly IStorageService _storageService;

    private static readonly Guid UserId  = Guid.Parse("aaaa0001-0000-0000-0000-000000000000");
    private static readonly Guid ShopId  = Guid.Parse("bbbb0001-0000-0000-0000-000000000000");
    private static readonly Guid OrderId = Guid.Parse("cccc0001-0000-0000-0000-000000000000");

    public MyFilesHandlerTests(ITestOutputHelper output)
    {
        _output            = output;
        _contentAccessRepo = Substitute.For<IContentAccessRepository>();
        _storageService    = Substitute.For<IStorageService>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Создаёт ContentAccess (через внутренний конструктор, доступный по рефлексии нет —
    /// используем публичный конструктор: ContentAccess(userId, contentFileId, orderId)).
    /// </summary>
    private static ContentAccess MakeAccess(Guid userId, Guid contentFileId, Guid orderId,
        ContentFile file)
    {
        var access = new ContentAccess(userId, contentFileId, orderId);

        // Подставляем ContentFile через рефлексию (приватное поле EF)
        typeof(ContentAccess)
            .GetProperty(nameof(ContentAccess.ContentFile))!
            .SetValue(access, file);

        return access;
    }

    private static ContentFile MakeFile(Guid id, string name, int productId, Product product)
    {
        var storage = new StorageObject($"key/{id}", $"{name}.zip", 1024, "application/zip", ShopId);
        var file = new ContentFile(5, name, storage.Id, productId);

        typeof(ContentFile).GetProperty("Id")!.SetValue(file, id);
        typeof(ContentFile).GetProperty("Product")!.SetValue(file, product);

        return file;
    }

    private static Product MakeProduct(int id, string name)
    {
        var product = new Product(name, "Desc", ShopId);
        typeof(Product).GetProperty("Id")!.SetValue(product, id);
        return product;
    }

    // ── Tests: GetMyFilesHandler ──────────────────────────────────────────────

    [Fact]
    public async Task GetMyFiles_MultipleProducts_GroupsByProduct()
    {
        var p1 = MakeProduct(1, "ChickenRoad Pack");
        var p2 = MakeProduct(2, "TwistXmas Bundle");

        var fileId1 = Guid.NewGuid();
        var fileId2 = Guid.NewGuid();
        var fileId3 = Guid.NewGuid();

        var f1 = MakeFile(fileId1, "symbols.zip", 1, p1);
        var f2 = MakeFile(fileId2, "sounds.zip",  1, p1);
        var f3 = MakeFile(fileId3, "xmas_bg.zip", 2, p2);

        var accesses = new List<ContentAccess>
        {
            MakeAccess(UserId, fileId1, OrderId, f1),
            MakeAccess(UserId, fileId2, OrderId, f2),
            MakeAccess(UserId, fileId3, OrderId, f3),
        };

        _contentAccessRepo.GetUserFilesAsync(UserId).Returns(accesses);

        var handler = new GetMyFilesHandler(_contentAccessRepo, _storageService);
        var result = await handler.Handle(new GetMyFilesQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Groups: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data!.Count);

        var group1 = result.Data.Single(g => g.ProductId == 1);
        Assert.Equal("ChickenRoad Pack", group1.ProductName);
        Assert.Equal(2, group1.Files.Count);

        var group2 = result.Data.Single(g => g.ProductId == 2);
        Assert.Equal("TwistXmas Bundle", group2.ProductName);
        Assert.Single(group2.Files);
    }

    [Fact]
    public async Task GetMyFiles_NoAccesses_ReturnsEmptyList()
    {
        _contentAccessRepo.GetUserFilesAsync(UserId).Returns(new List<ContentAccess>());

        var handler = new GetMyFilesHandler(_contentAccessRepo, _storageService);
        var result = await handler.Handle(new GetMyFilesQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Groups: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetMyFiles_SingleProduct_AllFilesInOneGroup()
    {
        var product = MakeProduct(10, "Mega Pack");

        var fileIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var files   = fileIds.Select(id => MakeFile(id, $"file_{id}", 10, product)).ToList();
        var accesses = files.Select(f => MakeAccess(UserId, f.Id, OrderId, f)).ToList();

        _contentAccessRepo.GetUserFilesAsync(UserId).Returns(accesses);

        var handler = new GetMyFilesHandler(_contentAccessRepo, _storageService);
        var result = await handler.Handle(new GetMyFilesQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Files in group: {result.Data?.FirstOrDefault()?.Files.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Single(result.Data!);
        Assert.Equal(5, result.Data![0].Files.Count);
    }

    [Fact]
    public async Task GetMyFiles_FilesHaveCorrectMetadata()
    {
        var product = MakeProduct(3, "SoundFX Pack");
        var fileId  = Guid.NewGuid();
        var file    = MakeFile(fileId, "effects.zip", 3, product);
        var access  = MakeAccess(UserId, fileId, OrderId, file);

        _contentAccessRepo.GetUserFilesAsync(UserId).Returns(new List<ContentAccess> { access });

        var handler = new GetMyFilesHandler(_contentAccessRepo, _storageService);
        var result = await handler.Handle(new GetMyFilesQuery(UserId), CancellationToken.None);

        var purchasedFile = result.Data![0].Files[0];
        Assert.Equal(fileId,  purchasedFile.ContentFileId);
        Assert.Equal("effects.zip", purchasedFile.FileName);
        Assert.Equal(OrderId, purchasedFile.OrderId);
    }

    [Fact]
    public async Task GetMyFiles_WhenRepositoryThrows_ReturnsError()
    {
        _contentAccessRepo.GetUserFilesAsync(UserId)
            .Throws(new Exception("DB failure"));

        var handler = new GetMyFilesHandler(_contentAccessRepo, _storageService);
        var result = await handler.Handle(new GetMyFilesQuery(UserId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB failure", result.ErrorMessage);
    }
}

using CreoHub.Application.Commands.StorageCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.StorageTests;

public class StorageCommandTests
{
    private readonly ITestOutputHelper _output;
    private readonly IMediaProductRepository _mediaProductRepo;
    private readonly IContentFileRepository _contentFileRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IThumbnailGenerationService _thumbnailService;

    private static readonly Guid ShopId      = Guid.Parse("ffff0001-0000-0000-0000-000000000000");
    private static readonly Guid OtherShopId = Guid.Parse("ffff0002-0000-0000-0000-000000000000");
    private const int ProductId = 7;

    public StorageCommandTests(ITestOutputHelper output)
    {
        _output            = output;
        _mediaProductRepo  = Substitute.For<IMediaProductRepository>();
        _contentFileRepo   = Substitute.For<IContentFileRepository>();
        _productRepo       = Substitute.For<IProductRepository>();
        _unitOfWork        = Substitute.For<IUnitOfWork>();
        _thumbnailService  = Substitute.For<IThumbnailGenerationService>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MediaProduct MakeMedia(int productId, Guid storageObjectId, int sortOrder = 0)
        => new MediaProduct(productId, storageObjectId, sortOrder);

    private static ContentFile MakeContentFile(int productId, int priceWeight = 5)
    {
        var storage = new StorageObject($"key/{productId}", "file.zip", 1024,
            "application/zip", ShopId);
        return new ContentFile(priceWeight, "original_name.zip", storage.Id, productId);
    }

    // ── Domain: MediaProduct.UpdateSortOrder / SetThumbnail ──────────────────

    [Fact]
    public void MediaProduct_UpdateSortOrder_PositiveValue_ChangesOrder()
    {
        var media = MakeMedia(ProductId, Guid.NewGuid(), sortOrder: 0);
        media.UpdateSortOrder(3);
        Assert.Equal(3, media.SortOrder);
    }

    [Fact]
    public void MediaProduct_UpdateSortOrder_Zero_Allowed()
    {
        var media = MakeMedia(ProductId, Guid.NewGuid(), sortOrder: 5);
        media.UpdateSortOrder(0);
        Assert.Equal(0, media.SortOrder);
    }

    [Fact]
    public void MediaProduct_UpdateSortOrder_Negative_Throws()
    {
        var media = MakeMedia(ProductId, Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => media.UpdateSortOrder(-1));
    }

    [Fact]
    public void MediaProduct_SetThumbnail_SetsId()
    {
        var media = MakeMedia(ProductId, Guid.NewGuid());
        var thumbId = Guid.NewGuid();
        media.SetThumbnail(thumbId);
        Assert.Equal(thumbId, media.ThumbnailId);
    }

    [Fact]
    public void MediaProduct_SetThumbnail_CanReplace()
    {
        var media = MakeMedia(ProductId, Guid.NewGuid());
        var first  = Guid.NewGuid();
        var second = Guid.NewGuid();
        media.SetThumbnail(first);
        media.SetThumbnail(second);
        Assert.Equal(second, media.ThumbnailId);
    }

    [Fact]
    public void MediaProduct_Constructor_NegativeSortOrder_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MediaProduct(ProductId, Guid.NewGuid(), sortOrder: -1));
    }

    // ── Domain: ContentFile.UpdatePreviewName / UpdatePriceWeight ────────────

    [Fact]
    public void ContentFile_UpdatePreviewName_ChangesName()
    {
        var file = MakeContentFile(ProductId);
        file.UpdatePreviewName("new_name.zip");
        Assert.Equal("new_name.zip", file.PreviewName);
    }

    [Fact]
    public void ContentFile_UpdatePreviewName_NullThrows()
    {
        var file = MakeContentFile(ProductId);
        Assert.Throws<ArgumentNullException>(() => file.UpdatePreviewName(null!));
    }

    [Fact]
    public void ContentFile_UpdatePriceWeight_ValidRange_Changes()
    {
        var file = MakeContentFile(ProductId, priceWeight: 3);
        file.UpdatePriceWeight(8);
        Assert.Equal(8, file.PriceWeight);
    }

    [Fact]
    public void ContentFile_UpdatePriceWeight_Zero_Throws()
    {
        var file = MakeContentFile(ProductId);
        Assert.Throws<ArgumentOutOfRangeException>(() => file.UpdatePriceWeight(0));
    }

    [Fact]
    public void ContentFile_UpdatePriceWeight_Eleven_Throws()
    {
        var file = MakeContentFile(ProductId);
        Assert.Throws<ArgumentOutOfRangeException>(() => file.UpdatePriceWeight(11));
    }

    [Fact]
    public void ContentFile_Constructor_WeightOutOfRange_Throws()
    {
        var storageId = Guid.NewGuid();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ContentFile(0, "name.zip", storageId, ProductId));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ContentFile(11, "name.zip", storageId, ProductId));
    }

    // ── Handler: BackfillThumbnailsHandler ────────────────────────────────────

    [Fact]
    public async Task BackfillThumbnails_NoVideos_ReturnsTotal0()
    {
        _mediaProductRepo.GetVideosWithoutThumbnailAsync(ShopId)
            .Returns(new List<MediaProduct>());

        var handler = new BackfillThumbnailsHandler(_mediaProductRepo, _thumbnailService);
        var result  = await handler.Handle(
            new BackfillThumbnailsCommand(ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Total: {result.Data?.Total}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(0, result.Data!.Total);
        Assert.Equal(0, result.Data.Succeeded);
        Assert.Equal(0, result.Data.Failed);
        await _thumbnailService.DidNotReceive()
            .GenerateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillThumbnails_ThreeVideos_CallsGenerateForEach()
    {
        var storageIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var candidates = storageIds
            .Select(id => MakeMedia(ProductId, id))
            .ToList();

        _mediaProductRepo.GetVideosWithoutThumbnailAsync(ShopId).Returns(candidates);
        _thumbnailService.GenerateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new BackfillThumbnailsHandler(_mediaProductRepo, _thumbnailService);
        var result  = await handler.Handle(
            new BackfillThumbnailsCommand(ShopId), CancellationToken.None);

        _output.WriteLine($"Total: {result.Data?.Total}, Succeeded: {result.Data?.Succeeded}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(3, result.Data!.Total);
        Assert.Equal(3, result.Data.Succeeded);
        Assert.Equal(0, result.Data.Failed);
        Assert.Empty(result.Data.Errors);
        await _thumbnailService.Received(3)
            .GenerateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackfillThumbnails_OneFailsOutOfThree_ContinuesBatch()
    {
        var goodId1 = Guid.NewGuid();
        var badId   = Guid.NewGuid();
        var goodId2 = Guid.NewGuid();

        var candidates = new List<MediaProduct>
        {
            MakeMedia(ProductId, goodId1),
            MakeMedia(ProductId, badId),
            MakeMedia(ProductId, goodId2),
        };

        _mediaProductRepo.GetVideosWithoutThumbnailAsync(ShopId).Returns(candidates);

        _thumbnailService.GenerateAsync(goodId1, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _thumbnailService.GenerateAsync(badId, Arg.Any<CancellationToken>())
            .Throws(new Exception("FFmpeg failed"));
        _thumbnailService.GenerateAsync(goodId2, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new BackfillThumbnailsHandler(_mediaProductRepo, _thumbnailService);
        var result  = await handler.Handle(
            new BackfillThumbnailsCommand(ShopId), CancellationToken.None);

        _output.WriteLine($"Total: {result.Data?.Total}, Succeeded: {result.Data?.Succeeded}, Failed: {result.Data?.Failed}");
        _output.WriteLine($"Errors: {string.Join("; ", result.Data?.Errors ?? [])}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(3, result.Data!.Total);
        Assert.Equal(2, result.Data.Succeeded);
        Assert.Equal(1, result.Data.Failed);
        Assert.Single(result.Data.Errors);
        Assert.Contains("FFmpeg failed", result.Data.Errors[0]);
    }

    [Fact]
    public async Task BackfillThumbnails_RepositoryThrows_ReturnsError()
    {
        _mediaProductRepo.GetVideosWithoutThumbnailAsync(ShopId)
            .Throws(new Exception("DB error"));

        var handler = new BackfillThumbnailsHandler(_mediaProductRepo, _thumbnailService);
        var result  = await handler.Handle(
            new BackfillThumbnailsCommand(ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB error", result.ErrorMessage);
    }

    // ── Handler: UpdateMediaSortOrderHandler ──────────────────────────────────

    [Fact]
    public async Task UpdateSortOrder_ValidRequest_CallsUpdateAndSave()
    {
        var storageId  = Guid.NewGuid();
        var media      = MakeMedia(ProductId, storageId, sortOrder: 0);

        _productRepo.GetShopIdByProductId(ProductId).Returns(ShopId);
        _mediaProductRepo.GetByProductAndStorageAsync(ProductId, storageId).Returns(media);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateMediaSortOrderHandler(_mediaProductRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateMediaSortOrderCommand(ShopId, ProductId, storageId, 3),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, SortOrder: {media.SortOrder}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        Assert.Equal(3, media.SortOrder);
        _mediaProductRepo.Received(1).Update(media);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSortOrder_WrongShop_ReturnsAccessDenied()
    {
        var storageId = Guid.NewGuid();
        _productRepo.GetShopIdByProductId(ProductId).Returns(OtherShopId);

        var handler = new UpdateMediaSortOrderHandler(_mediaProductRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateMediaSortOrderCommand(ShopId, ProductId, storageId, 1),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSortOrder_MediaNotFound_ReturnsError()
    {
        var storageId = Guid.NewGuid();
        _productRepo.GetShopIdByProductId(ProductId).Returns(ShopId);
        _mediaProductRepo.GetByProductAndStorageAsync(ProductId, storageId).ReturnsNull();

        var handler = new UpdateMediaSortOrderHandler(_mediaProductRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateMediaSortOrderCommand(ShopId, ProductId, storageId, 2),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Handler: UpdateContentFileHandler ─────────────────────────────────────

    [Fact]
    public async Task UpdateContentFile_NameOnly_UpdatesNameNotWeight()
    {
        var fileId  = Guid.NewGuid();
        var file    = MakeContentFile(ProductId, priceWeight: 5);
        typeof(ContentFile).GetProperty("Id")!.SetValue(file, fileId);

        _contentFileRepo.GetByIdAsync(fileId).Returns(file);
        _productRepo.GetShopIdByProductId(ProductId).Returns(ShopId);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var dto = new UpdateContentFileDTO { PreviewName = "renamed.zip", PriceWeight = null };

        var handler = new UpdateContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateContentFileCommand(ShopId, fileId, dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Name: {file.PreviewName}, Weight: {file.PriceWeight}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("renamed.zip", file.PreviewName);
        Assert.Equal(5, file.PriceWeight); // не изменился
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentFile_WeightOnly_UpdatesWeightNotName()
    {
        var fileId  = Guid.NewGuid();
        var file    = MakeContentFile(ProductId, priceWeight: 3);
        typeof(ContentFile).GetProperty("Id")!.SetValue(file, fileId);
        var originalName = file.PreviewName;

        _contentFileRepo.GetByIdAsync(fileId).Returns(file);
        _productRepo.GetShopIdByProductId(ProductId).Returns(ShopId);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var dto = new UpdateContentFileDTO { PreviewName = null, PriceWeight = 9 };

        var handler = new UpdateContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateContentFileCommand(ShopId, fileId, dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Name: {file.PreviewName}, Weight: {file.PriceWeight}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(originalName, file.PreviewName); // не изменилось
        Assert.Equal(9, file.PriceWeight);
    }

    [Fact]
    public async Task UpdateContentFile_BothFields_UpdatesBoth()
    {
        var fileId = Guid.NewGuid();
        var file   = MakeContentFile(ProductId, priceWeight: 2);
        typeof(ContentFile).GetProperty("Id")!.SetValue(file, fileId);

        _contentFileRepo.GetByIdAsync(fileId).Returns(file);
        _productRepo.GetShopIdByProductId(ProductId).Returns(ShopId);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var dto = new UpdateContentFileDTO { PreviewName = "final.zip", PriceWeight = 7 };

        var handler = new UpdateContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateContentFileCommand(ShopId, fileId, dto), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("final.zip", file.PreviewName);
        Assert.Equal(7, file.PriceWeight);
    }

    [Fact]
    public async Task UpdateContentFile_FileNotFound_ReturnsError()
    {
        var fileId = Guid.NewGuid();
        _contentFileRepo.GetByIdAsync(fileId).ReturnsNull();

        var handler = new UpdateContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateContentFileCommand(ShopId, fileId,
                new UpdateContentFileDTO { PreviewName = "x" }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentFile_WrongShop_ReturnsAccessDenied()
    {
        var fileId = Guid.NewGuid();
        var file   = MakeContentFile(ProductId);
        typeof(ContentFile).GetProperty("Id")!.SetValue(file, fileId);

        _contentFileRepo.GetByIdAsync(fileId).Returns(file);
        _productRepo.GetShopIdByProductId(ProductId).Returns(OtherShopId);

        var handler = new UpdateContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateContentFileCommand(ShopId, fileId,
                new UpdateContentFileDTO { PreviewName = "hack.zip" }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("denied", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContentFile_InvalidWeight_ReturnsError()
    {
        var fileId = Guid.NewGuid();
        var file   = MakeContentFile(ProductId, priceWeight: 5);
        typeof(ContentFile).GetProperty("Id")!.SetValue(file, fileId);

        _contentFileRepo.GetByIdAsync(fileId).Returns(file);
        _productRepo.GetShopIdByProductId(ProductId).Returns(ShopId);

        var dto = new UpdateContentFileDTO { PriceWeight = 99 }; // вне диапазона

        var handler = new UpdateContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork);
        var result  = await handler.Handle(
            new UpdateContentFileCommand(ShopId, fileId, dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");

        Assert.Equal(ResponseStatus.Error, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

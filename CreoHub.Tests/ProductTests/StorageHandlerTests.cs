using CreoHub.Application.Commands.StorageCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ProductTests;

public class StorageHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IStorageObjectRepository _storageRepo;
    private readonly IMediaProductRepository _mediaProductRepo;
    private readonly IProductRepository _productRepo;
    private readonly IContentFileRepository _contentFileRepo;
    private readonly IStorageService _storageService;
    private readonly IVideoOptimizationQueueService _queue;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid ShopId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const int ProductId = 7;

    public StorageHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _storageRepo = Substitute.For<IStorageObjectRepository>();
        _mediaProductRepo = Substitute.For<IMediaProductRepository>();
        _productRepo = Substitute.For<IProductRepository>();
        _contentFileRepo = Substitute.For<IContentFileRepository>();
        _storageService = Substitute.For<IStorageService>();
        _queue = Substitute.For<IVideoOptimizationQueueService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private StorageObject MakeStorage(Guid? ownerId = null, string mime = "image/webp", long size = 1_000_000)
    {
        var so = new StorageObject("files/test.webp", "test.webp", size, mime, ownerId ?? ShopId);
        return so;
    }

    private Product MakeProduct(Guid? ownerId = null)
        => new Product("Test Product", "Desc", ownerId ?? ShopId, null);

    // ─── AttachMedia ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AttachMedia_ValidData_ReturnsSuccess()
    {
        var storageId = Guid.NewGuid();
        var storage = MakeStorage();                   // OwnerId == ShopId, MediaProduct == null
        var product = MakeProduct();                   // OwnerId == ShopId

        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(product));
        _mediaProductRepo.AddAsync(Arg.Any<MediaProduct>()).Returns(ci => Task.FromResult(ci.Arg<MediaProduct>()));
        _storageRepo.Update(Arg.Any<StorageObject>()).Returns(storage);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new AttachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo, _productRepo);
        var result = await handler.Handle(new AttachMedia(ShopId, ProductId, storageId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(FileType.Media, storage.FileType);
    }

    [Fact]
    public async Task AttachMedia_FileNotFound_ReturnsFail()
    {
        _storageRepo.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(MakeProduct()));

        var handler = new AttachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo, _productRepo);
        var result = await handler.Handle(new AttachMedia(ShopId, ProductId, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Файл не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task AttachMedia_WrongOwner_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        var otherShop = Guid.NewGuid();
        var storage = MakeStorage(otherShop);          // belongs to different shop

        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(MakeProduct()));

        var handler = new AttachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo, _productRepo);
        var result = await handler.Handle(new AttachMedia(ShopId, ProductId, storageId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не принадлежит вам", result.ErrorMessage);
    }

    [Fact]
    public async Task AttachMedia_ProductNotFound_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(MakeStorage()));
        _productRepo.GetByIdAsync(ProductId).ReturnsNull();

        var handler = new AttachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo, _productRepo);
        var result = await handler.Handle(new AttachMedia(ShopId, ProductId, storageId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Продукт не был найден", result.ErrorMessage);
    }

    [Fact]
    public async Task AttachMedia_ProductOwnerMismatch_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        var otherShop = Guid.NewGuid();
        var storage = MakeStorage();                   // OwnerId == ShopId
        var product = MakeProduct(otherShop);          // OwnerId == different shop

        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(product));

        var handler = new AttachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo, _productRepo);
        var result = await handler.Handle(new AttachMedia(ShopId, ProductId, storageId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ─── DetachMedia ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DetachMedia_FileNotFound_ReturnsFail()
    {
        _storageRepo.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var handler = new DetachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo);
        var result = await handler.Handle(new DetachMedia(ShopId, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Файл не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task DetachMedia_WrongOwner_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        var otherShop = Guid.NewGuid();
        var storage = MakeStorage(otherShop);
        // Inject a MediaProduct via ChangeFileType trick and reflection is complex,
        // but OwnerId check happens after MediaProduct check — let's set MediaProduct via reflection
        SetProperty(storage, "MediaProduct", new MediaProduct(ProductId, storageId, 0));

        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new DetachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo);
        var result = await handler.Handle(new DetachMedia(ShopId, storageId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не принадлежит вам", result.ErrorMessage);
    }

    [Fact]
    public async Task DetachMedia_NoMediaProduct_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        var storage = MakeStorage();   // MediaProduct is null by default

        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new DetachMediaHandler(_unitOfWork, _storageRepo, _mediaProductRepo);
        var result = await handler.Handle(new DetachMedia(ShopId, storageId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("медиа", result.ErrorMessage);
    }

    // ─── DeleteFile ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteFile_UnregisteredFile_ReturnsSuccess()
    {
        var fileId = Guid.NewGuid();
        var storage = MakeStorage();   // FileType.Unregistred by default
        _storageRepo.GetByIdAsync(fileId).Returns(Task.FromResult<StorageObject?>(storage));
        _storageRepo.Remove(Arg.Any<StorageObject>());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _storageService.DeleteFileAsync(storage.Key).Returns(Task.FromResult(true));

        var handler = new DeleteFileHandler(_storageRepo, _unitOfWork, _storageService);
        var result = await handler.Handle(new DeleteFile(fileId, ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        _storageRepo.Received(1).Remove(storage);
        await _storageService.Received(1).DeleteFileAsync(storage.Key);
    }

    [Fact]
    public async Task DeleteFile_FileInUseAsMedia_ReturnsFail()
    {
        var fileId = Guid.NewGuid();
        var storage = MakeStorage();
        storage.ChangeFileType(FileType.Media);

        _storageRepo.GetByIdAsync(fileId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new DeleteFileHandler(_storageRepo, _unitOfWork, _storageService);
        var result = await handler.Handle(new DeleteFile(fileId, ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("используется", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteFile_WrongOwner_ReturnsFail()
    {
        var fileId = Guid.NewGuid();
        var otherShop = Guid.NewGuid();
        var storage = MakeStorage(otherShop);    // Unregistered but belongs to other shop

        _storageRepo.GetByIdAsync(fileId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new DeleteFileHandler(_storageRepo, _unitOfWork, _storageService);
        var result = await handler.Handle(new DeleteFile(fileId, ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("владельцем", result.ErrorMessage);
    }

    // ─── AttachContentFile ────────────────────────────────────────────────────

    [Fact]
    public async Task AttachContentFile_ValidData_ReturnsSuccess()
    {
        var storageId = Guid.NewGuid();
        var product = MakeProduct();
        var storage = MakeStorage();

        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(product));
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));
        _storageRepo.Update(Arg.Any<StorageObject>()).Returns(storage);
        _contentFileRepo.AddAsync(Arg.Any<ContentFile>()).Returns(ci => Task.FromResult(ci.Arg<ContentFile>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new AttachContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork, _storageRepo);
        var result = await handler.Handle(
            new AttachContentFileCommand(ShopId, new AttachContentFileDTO
            {
                ProductId = ProductId,
                StorageObjectId = storageId,
                PriceWeight = 3,
                PreviewName = "animation_pack.zip"
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(FileType.Content, storage.FileType);
    }

    [Fact]
    public async Task AttachContentFile_ProductNotFound_ReturnsFail()
    {
        _productRepo.GetByIdAsync(ProductId).ReturnsNull();

        var handler = new AttachContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork, _storageRepo);
        var result = await handler.Handle(
            new AttachContentFileCommand(ShopId, new AttachContentFileDTO
            {
                ProductId = ProductId, StorageObjectId = Guid.NewGuid(), PriceWeight = 1, PreviewName = "f.zip"
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Товар не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task AttachContentFile_ProductWrongOwner_ReturnsFail()
    {
        var otherShop = Guid.NewGuid();
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(MakeProduct(otherShop)));

        var handler = new AttachContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork, _storageRepo);
        var result = await handler.Handle(
            new AttachContentFileCommand(ShopId, new AttachContentFileDTO
            {
                ProductId = ProductId, StorageObjectId = Guid.NewGuid(), PriceWeight = 1, PreviewName = "f.zip"
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("нет прав", result.ErrorMessage);
    }

    [Fact]
    public async Task AttachContentFile_StorageNotFound_ReturnsFail()
    {
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(MakeProduct()));
        _storageRepo.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var handler = new AttachContentFileHandler(_contentFileRepo, _productRepo, _unitOfWork, _storageRepo);
        var result = await handler.Handle(
            new AttachContentFileCommand(ShopId, new AttachContentFileDTO
            {
                ProductId = ProductId, StorageObjectId = Guid.NewGuid(), PriceWeight = 1, PreviewName = "f.zip"
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Файл не найден", result.ErrorMessage);
    }

    // ─── DetachContentFile ────────────────────────────────────────────────────

    [Fact]
    public async Task DetachContentFile_LastFile_UnregistersStorageAndReturnsSuccess()
    {
        var contentFileId = Guid.NewGuid();
        var storageId = Guid.NewGuid();

        var product = MakeProduct();
        var storage = MakeStorage();
        storage.ChangeFileType(FileType.Content);
        var contentFile = new ContentFile(2, "preview.png", storageId, ProductId);

        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(product));
        _contentFileRepo.GetByIdAsync(contentFileId).Returns(Task.FromResult<ContentFile?>(contentFile));
        // Handler looks up storage by contentFile.StorageObjectId (= storageId), not by storage.Id
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));
        // Handler calls GetByStorageObjectIdAsync(storageObject.Id) — storage.Id is auto-generated, use Arg.Any
        _contentFileRepo.GetByStorageObjectIdAsync(Arg.Any<Guid>()).Returns(Task.FromResult(new List<ContentFile> { contentFile }));
        _contentFileRepo.GetActiveCountByProductIdAsync(ProductId).Returns(Task.FromResult(2)); // product has 2 files → detach allowed
        _contentFileRepo.HasPurchasesAsync(Arg.Any<Guid>()).Returns(Task.FromResult(false));    // no purchases → clean delete path
        _contentFileRepo.Remove(Arg.Any<ContentFile>());
        _productRepo.Update(Arg.Any<Product>()).Returns(product);
        _storageRepo.Update(Arg.Any<StorageObject>()).Returns(storage);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new DetachContentFileHandler(_storageRepo, _unitOfWork, _contentFileRepo, _productRepo);
        var result = await handler.Handle(
            new DetachContentFileCommand(ShopId, new DetachContentFileDTO
            {
                ProductId = ProductId, ContentFileId = contentFileId
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(FileType.Unregistred, storage.FileType);
    }

    [Fact]
    public async Task DetachContentFile_ProductNotFound_ReturnsFail()
    {
        _productRepo.GetByIdAsync(ProductId).ReturnsNull();

        var handler = new DetachContentFileHandler(_storageRepo, _unitOfWork, _contentFileRepo, _productRepo);
        var result = await handler.Handle(
            new DetachContentFileCommand(ShopId, new DetachContentFileDTO
            {
                ProductId = ProductId, ContentFileId = Guid.NewGuid()
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Product not found", result.ErrorMessage);
    }

    [Fact]
    public async Task DetachContentFile_WrongOwner_ReturnsFail()
    {
        var otherShop = Guid.NewGuid();
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(MakeProduct(otherShop)));

        var handler = new DetachContentFileHandler(_storageRepo, _unitOfWork, _contentFileRepo, _productRepo);
        var result = await handler.Handle(
            new DetachContentFileCommand(ShopId, new DetachContentFileDTO
            {
                ProductId = ProductId, ContentFileId = Guid.NewGuid()
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("cannot edit", result.ErrorMessage);
    }

    [Fact]
    public async Task DetachContentFile_ContentFileNotFound_ReturnsFail()
    {
        _productRepo.GetByIdAsync(ProductId).Returns(Task.FromResult<Product?>(MakeProduct()));
        _contentFileRepo.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var handler = new DetachContentFileHandler(_storageRepo, _unitOfWork, _contentFileRepo, _productRepo);
        var result = await handler.Handle(
            new DetachContentFileCommand(ShopId, new DetachContentFileDTO
            {
                ProductId = ProductId, ContentFileId = Guid.NewGuid()
            }),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Content file not found", result.ErrorMessage);
    }

    // ─── OptimizeStorageObject ────────────────────────────────────────────────

    [Fact]
    public async Task OptimizeStorageObject_ValidMp4_EnqueuesAndReturnsSuccess()
    {
        var storageId = Guid.NewGuid();
        // 50 MB mp4 — above threshold, unregistered
        var storage = MakeStorage(mime: "video/mp4", size: 50 * 1024 * 1024);
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));

        _queue.TryEnqueue(storageId).Returns(true);

        var handler = new OptimizeStorageObjectHandler(_storageRepo, _queue, _unitOfWork);
        var result = await handler.Handle(new OptimizeStorageObjectCommand(storageId, ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        _queue.Received(1).TryEnqueue(storageId);
    }

    [Fact]
    public async Task OptimizeStorageObject_FileNotFound_ReturnsFail()
    {
        _storageRepo.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var handler = new OptimizeStorageObjectHandler(_storageRepo, _queue, _unitOfWork);
        var result = await handler.Handle(
            new OptimizeStorageObjectCommand(Guid.NewGuid(), ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Файл не найден", result.ErrorMessage);
        _queue.DidNotReceive().TryEnqueue(Arg.Any<Guid>());
    }

    [Fact]
    public async Task OptimizeStorageObject_WrongOwner_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        var storage = MakeStorage(Guid.NewGuid(), "video/mp4", 50_000_000);   // different shop
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new OptimizeStorageObjectHandler(_storageRepo, _queue, _unitOfWork);
        var result = await handler.Handle(new OptimizeStorageObjectCommand(storageId, ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не принадлежит", result.ErrorMessage);
    }

    [Fact]
    public async Task OptimizeStorageObject_NotVideo_ReturnsFail()
    {
        // Оптимизация принимает любое видео (mp4/webm/…); не-видео отклоняется.
        // mp4-only и min-5MB проверки убраны в d3dd73a.
        var storageId = Guid.NewGuid();
        var storage = MakeStorage(mime: "image/png", size: 50_000_000);
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new OptimizeStorageObjectHandler(_storageRepo, _queue, _unitOfWork);
        var result = await handler.Handle(new OptimizeStorageObjectCommand(storageId, ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("не является видео", result.ErrorMessage);
    }

    [Fact]
    public async Task OptimizeStorageObject_ValidVideo_QueuesSuccessfully()
    {
        // Любое видео в статусе None успешно ставится в очередь оптимизации.
        var storageId = Guid.NewGuid();
        var storage = MakeStorage(mime: "video/mp4", size: 50_000_000);
        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));
        _queue.TryEnqueue(storageId).Returns(true);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new OptimizeStorageObjectHandler(_storageRepo, _queue, _unitOfWork);
        var result = await handler.Handle(new OptimizeStorageObjectCommand(storageId, ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(VideoOptimizationStatus.Queued, storage.VideoOptimizationStatus);
    }

    [Fact]
    public async Task OptimizeStorageObject_ContentFile_ReturnsFail()
    {
        var storageId = Guid.NewGuid();
        var storage = MakeStorage(mime: "video/mp4", size: 50_000_000);
        storage.ChangeFileType(FileType.Content);

        _storageRepo.GetByIdAsync(storageId).Returns(Task.FromResult<StorageObject?>(storage));

        var handler = new OptimizeStorageObjectHandler(_storageRepo, _queue, _unitOfWork);
        var result = await handler.Handle(new OptimizeStorageObjectCommand(storageId, ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("контента", result.ErrorMessage);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void SetProperty(object obj, string propName, object? value)
    {
        var prop = obj.GetType().GetProperty(propName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }
}

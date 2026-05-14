using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

/// <summary>
/// Uploads an image to R2, saves it as a StorageObject with FileType.Decoration,
/// then assigns it as the shop's banner or logo depending on <paramref name="Slot"/>.
/// </summary>
public record UpdateShopDecorationsCommand(
    Stream     FileStream,
    string     FileName,
    string     MimeType,
    string     Slot,       // "banner" | "logo"
    Guid       ShopId
) : IRequest<BaseResponse<string>>;

public class UpdateShopDecorationsHandler : IRequestHandler<UpdateShopDecorationsCommand, BaseResponse<string>>
{
    private readonly IShopRepository          _shopRepository;
    private readonly IStorageObjectRepository _storageRepository;
    private readonly IStorageService          _storageService;
    private readonly IUnitOfWork             _unitOfWork;

    public UpdateShopDecorationsHandler(
        IShopRepository          shopRepository,
        IStorageObjectRepository storageRepository,
        IStorageService          storageService,
        IUnitOfWork              unitOfWork)
    {
        _shopRepository    = shopRepository;
        _storageRepository = storageRepository;
        _storageService    = storageService;
        _unitOfWork        = unitOfWork;
    }

    public async Task<BaseResponse<string>> Handle(UpdateShopDecorationsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Upload to R2
            var key      = $"{request.ShopId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";
            var fileSize = request.FileStream.Length;

            await _storageService.UploadFileAsync(request.FileStream, key, request.MimeType);

            // 2. Create StorageObject with Decoration type
            var obj = new StorageObject(key, request.FileName, fileSize, request.MimeType, request.ShopId);
            obj.ChangeFileType(FileType.Decoration);
            var saved = await _storageRepository.AddAsync(obj);

            // 3. Load shop and update the right FK
            var shop = await _shopRepository.GetShopByIdAsync(request.ShopId);
            if (shop is null)
                return BaseResponse<string>.Fail("Магазин не найден");

            var bannerId = request.Slot == "banner" ? saved.Id : shop.BannerId;
            var logoId   = request.Slot == "logo"   ? saved.Id : shop.LogoId;
            shop.UpdateDecorations(bannerId, logoId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<string>.Success(key);
        }
        catch (Exception ex)
        {
            return BaseResponse<string>.Fail(ex.Message);
        }
    }
}

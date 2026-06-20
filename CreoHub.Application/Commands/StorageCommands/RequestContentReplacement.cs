using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

/// <summary>
/// Продавец просит заменить контент-файл новым (уже загруженным) файлом.
/// Заявка уходит на модерацию — старый файл не трогается до аппрува.
/// </summary>
public record RequestContentReplacementCommand(Guid ShopId, Guid ContentFileId, Guid NewStorageObjectId)
    : IRequest<BaseResponse<bool>>;

public class RequestContentReplacementHandler
    : IRequestHandler<RequestContentReplacementCommand, BaseResponse<bool>>
{
    private readonly IContentFileRepository            _contentFiles;
    private readonly IStorageObjectRepository          _storageObjects;
    private readonly IContentFileReplacementRepository _replacements;
    private readonly IUnitOfWork                       _unitOfWork;

    public RequestContentReplacementHandler(
        IContentFileRepository contentFiles,
        IStorageObjectRepository storageObjects,
        IContentFileReplacementRepository replacements,
        IUnitOfWork unitOfWork)
    {
        _contentFiles   = contentFiles;
        _storageObjects = storageObjects;
        _replacements   = replacements;
        _unitOfWork     = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(RequestContentReplacementCommand request, CancellationToken ct)
    {
        try
        {
            var contentFile = await _contentFiles.GetByIdWithStorageAsync(request.ContentFileId);
            if (contentFile?.Product is null)
                return BaseResponse<bool>.Fail("Контент-файл не найден.");
            if (contentFile.Product.OwnerId != request.ShopId)
                return BaseResponse<bool>.Fail("Нет доступа: товар принадлежит другому магазину.");

            var newFile = await _storageObjects.GetByIdAsync(request.NewStorageObjectId);
            if (newFile is null)
                return BaseResponse<bool>.Fail("Новый файл не найден.");
            if (newFile.OwnerId != request.ShopId)
                return BaseResponse<bool>.Fail("Нет доступа: новый файл принадлежит другому магазину.");

            if (await _replacements.HasPendingAsync(request.ContentFileId, ct))
                return BaseResponse<bool>.Fail("По этому файлу уже есть заявка на замену на модерации.");

            await _replacements.AddAsync(
                ContentFileReplacement.Create(request.ContentFileId, request.ShopId, request.NewStorageObjectId), ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record ConfirmStorageUploadCommand(
    string Key,
    string FileName,
    string MimeType,
    long   FileSize,
    Guid   ShopId
) : IRequest<BaseResponse<StorageObject>>;

public class ConfirmStorageUploadHandler
    : IRequestHandler<ConfirmStorageUploadCommand, BaseResponse<StorageObject>>
{
    private readonly IStorageService          _storageService;
    private readonly IStorageObjectRepository _objectRepository;
    private readonly IUnitOfWork              _unitOfWork;

    public ConfirmStorageUploadHandler(
        IStorageService          storageService,
        IStorageObjectRepository objectRepository,
        IUnitOfWork              unitOfWork)
    {
        _storageService   = storageService;
        _objectRepository = objectRepository;
        _unitOfWork       = unitOfWork;
    }

    public async Task<BaseResponse<StorageObject>> Handle(
        ConfirmStorageUploadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Убедиться что файл реально загружен в R2
            var exists = await _storageService.FileExistsAsync(request.Key);
            if (!exists)
                return BaseResponse<StorageObject>.Fail(
                    "Файл не найден в хранилище. Загрузка не была завершена.");

            var storageObject = new StorageObject(
                key:      request.Key,
                fileName: request.FileName,
                fileSize: request.FileSize,
                mimeType: request.MimeType,
                ownerId:  request.ShopId
            );

            await _objectRepository.AddAsync(storageObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<StorageObject>.Success(storageObject);
        }
        catch (Exception ex)
        {
            return BaseResponse<StorageObject>.Fail(ex.Message);
        }
    }
}

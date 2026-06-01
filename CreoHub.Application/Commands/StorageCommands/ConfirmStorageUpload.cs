using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;
using System.Collections.Generic;

namespace CreoHub.Application.Commands.StorageCommands;

public record ConfirmStorageUploadCommand(
    string Key,
    string FileName,
    string MimeType,
    long   FileSize,
    Guid   ShopId
) : IRequest<BaseResponse<StorageObject>>;

// Форматы видео которые браузеры не воспроизводят нативно — конвертируем в MP4 автоматически
internal static class NonBrowserVideoMimes
{
    public static readonly HashSet<string> Set = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/quicktime",   // .mov
        "video/x-msvideo",   // .avi
        "video/x-ms-wmv",    // .wmv
        "video/x-matroska",  // .mkv
    };
}

public class ConfirmStorageUploadHandler
    : IRequestHandler<ConfirmStorageUploadCommand, BaseResponse<StorageObject>>
{
    private readonly IStorageService                 _storageService;
    private readonly IStorageObjectRepository        _objectRepository;
    private readonly IVideoOptimizationQueueService  _conversionQueue;
    private readonly IUnitOfWork                     _unitOfWork;

    public ConfirmStorageUploadHandler(
        IStorageService                storageService,
        IStorageObjectRepository       objectRepository,
        IVideoOptimizationQueueService conversionQueue,
        IUnitOfWork                    unitOfWork)
    {
        _storageService   = storageService;
        _objectRepository = objectRepository;
        _conversionQueue  = conversionQueue;
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

            // Видео не поддерживаемых браузерами форматов — авто-конвертация в MP4
            if (NonBrowserVideoMimes.Set.Contains(request.MimeType))
                storageObject.MarkQueued();

            await _objectRepository.AddAsync(storageObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Поставить в очередь ПОСЛЕ сохранения в БД (нужен Id)
            if (NonBrowserVideoMimes.Set.Contains(request.MimeType))
                _conversionQueue.TryEnqueue(storageObject.Id);

            return BaseResponse<StorageObject>.Success(storageObject);
        }
        catch (Exception ex)
        {
            return BaseResponse<StorageObject>.Fail(ex.Message);
        }
    }
}

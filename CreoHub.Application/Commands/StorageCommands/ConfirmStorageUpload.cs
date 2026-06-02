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
    private readonly IPendingUploadRepository        _pendingUploads;

    public ConfirmStorageUploadHandler(
        IStorageService                storageService,
        IStorageObjectRepository       objectRepository,
        IVideoOptimizationQueueService conversionQueue,
        IUnitOfWork                    unitOfWork,
        IPendingUploadRepository       pendingUploads)
    {
        _storageService   = storageService;
        _objectRepository = objectRepository;
        _conversionQueue  = conversionQueue;
        _unitOfWork       = unitOfWork;
        _pendingUploads   = pendingUploads;
    }

    public async Task<BaseResponse<StorageObject>> Handle(
        ConfirmStorageUploadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ── Проверяем pending-запись: key должен принадлежать этому магазину ──
            var pending = await _pendingUploads.ConsumeAsync(request.Key, request.ShopId, cancellationToken);
            if (pending is null)
                return BaseResponse<StorageObject>.Fail(
                    "Загрузка не авторизована или истекло время ожидания. Запросите новый URL.");

            // FileName/MimeType берём из pending-записи, а не от клиента.
            // FileSize клиента (request.FileSize) игнорируем — берём реальный из R2.
            var trustedMimeType = pending.MimeType;

            // ── HEAD к R2: наличие + реальный размер (источник правды) ────────
            var (exists, contentLength, _) = await _storageService.GetObjectMetadataAsync(request.Key);
            if (!exists)
                return BaseResponse<StorageObject>.Fail(
                    "Файл не найден в хранилище. Загрузка не была завершена.");

            // Presigned PUT не ограничивает реальный Content-Length — клиент мог запросить
            // URL под маленький файл и залить большой. Сверяем с лимитом, зафиксированным
            // на request-upload; при превышении удаляем orphan и отклоняем.
            if (pending.MaxBytes > 0 && contentLength > pending.MaxBytes)
            {
                await _storageService.DeleteFileAsync(request.Key);
                return BaseResponse<StorageObject>.Fail("Файл превышает допустимый размер.");
            }

            var storageObject = new StorageObject(
                key:      request.Key,
                fileName: pending.FileName,   // из pending, не от клиента
                fileSize: contentLength,      // реальный размер из R2, не от клиента
                mimeType: trustedMimeType,    // из pending, не от клиента
                ownerId:  request.ShopId
            );

            // Видео не поддерживаемых браузерами форматов — авто-конвертация в MP4
            if (NonBrowserVideoMimes.Set.Contains(trustedMimeType))
                storageObject.MarkQueued();

            await _objectRepository.AddAsync(storageObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Поставить в очередь ПОСЛЕ сохранения в БД (нужен Id)
            if (NonBrowserVideoMimes.Set.Contains(trustedMimeType))
                _conversionQueue.TryEnqueue(storageObject.Id);

            return BaseResponse<StorageObject>.Success(storageObject);
        }
        catch (Exception ex)
        {
            return BaseResponse<StorageObject>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record RequestStorageUploadResult(string Key, string UploadUrl);

public record RequestStorageUploadCommand(
    string FileName,
    string MimeType,
    long   FileSize,
    Guid   ShopId,
    long   MaxBytes
) : IRequest<BaseResponse<RequestStorageUploadResult>>;

public class RequestStorageUploadHandler
    : IRequestHandler<RequestStorageUploadCommand, BaseResponse<RequestStorageUploadResult>>
{
    private readonly IStorageService         _storageService;
    private readonly IShopRepository         _shopRepository;
    private readonly IPendingUploadRepository _pendingUploads;

    public RequestStorageUploadHandler(
        IStorageService         storageService,
        IShopRepository         shopRepository,
        IPendingUploadRepository pendingUploads)
    {
        _storageService = storageService;
        _shopRepository = shopRepository;
        _pendingUploads = pendingUploads;
    }

    public async Task<BaseResponse<RequestStorageUploadResult>> Handle(
        RequestStorageUploadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            var key = $"{request.ShopId}/{Guid.NewGuid()}{ext}";

            var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(key, request.MimeType);

            // Сохраняем pending-запись чтобы confirm-upload мог проверить, что key
            // принадлежит этому магазину и не был подменён клиентом. FileName/MimeType/MaxBytes
            // фиксируем здесь (серверный источник правды) — на confirm клиенту не доверяем.
            var pending = PendingUpload.Create(
                key, request.ShopId, request.FileName, request.MimeType, request.MaxBytes);
            await _pendingUploads.AddAsync(pending, cancellationToken);

            return BaseResponse<RequestStorageUploadResult>.Success(new(key, uploadUrl));
        }
        catch (Exception ex)
        {
            return BaseResponse<RequestStorageUploadResult>.Fail(ex.Message);
        }
    }
}

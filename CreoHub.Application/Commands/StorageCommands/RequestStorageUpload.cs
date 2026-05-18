using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record RequestStorageUploadResult(string Key, string UploadUrl);

public record RequestStorageUploadCommand(
    string FileName,
    string MimeType,
    long   FileSize,
    Guid   ShopId
) : IRequest<BaseResponse<RequestStorageUploadResult>>;

public class RequestStorageUploadHandler
    : IRequestHandler<RequestStorageUploadCommand, BaseResponse<RequestStorageUploadResult>>
{
    private readonly IStorageService  _storageService;
    private readonly IShopRepository  _shopRepository;

    public RequestStorageUploadHandler(IStorageService storageService, IShopRepository shopRepository)
    {
        _storageService = storageService;
        _shopRepository = shopRepository;
    }

    public async Task<BaseResponse<RequestStorageUploadResult>> Handle(
        RequestStorageUploadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            var key = $"{request.ShopId}/{Guid.NewGuid()}{ext}";

            var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(key, request.MimeType);

            return BaseResponse<RequestStorageUploadResult>.Success(new(key, uploadUrl));
        }
        catch (Exception ex)
        {
            return BaseResponse<RequestStorageUploadResult>.Fail(ex.Message);
        }
    }
}

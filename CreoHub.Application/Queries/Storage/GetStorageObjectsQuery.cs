using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Storage;

public record GetStorageObjectsQuery(
    Guid    ShopId,
    int     Page     = 1,
    int     PageSize = 20,
    string? Search   = null,
    string? Type     = null,   // "image" | "video" | "other" | null = все
    string  Sort     = "date",
    string  Order    = "desc"
) : IRequest<BaseResponse<StorageObjectsPagedResponseDTO>>;

public class GetStorageObjectHandler
    : IRequestHandler<GetStorageObjectsQuery, BaseResponse<StorageObjectsPagedResponseDTO>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IStorageService          _storageService;

    public GetStorageObjectHandler(
        IStorageObjectRepository storageObjectRepository,
        IStorageService          storageService)
    {
        _storageObjectRepository = storageObjectRepository;
        _storageService          = storageService;
    }

    public async Task<BaseResponse<StorageObjectsPagedResponseDTO>> Handle(
        GetStorageObjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var page     = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var result = await _storageObjectRepository.GetPagedByShopIdAsync(
                request.ShopId,
                page,
                pageSize,
                request.Search,
                request.Type,
                request.Sort,
                request.Order);

            // Генерируем presigned URL для каждого элемента страницы
            foreach (var obj in result.Items)
            {
                if (!string.IsNullOrEmpty(obj.Key))
                    obj.Key = _storageService.GeneratePresignedUrl(obj.Key, 60);
            }

            return BaseResponse<StorageObjectsPagedResponseDTO>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<StorageObjectsPagedResponseDTO>.Fail(ex.Message);
        }
    }
}

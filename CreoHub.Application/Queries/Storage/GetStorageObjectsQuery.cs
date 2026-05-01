using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Storage;

public record GetStorageObjectsQuery(Guid shopId) : IRequest<BaseResponse<List<StorageObjectResponseDTO>>>;

public class GetStorageObjectHandler : IRequestHandler<GetStorageObjectsQuery, BaseResponse<List<StorageObjectResponseDTO>>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IStorageService _storageService;

    public GetStorageObjectHandler(IStorageObjectRepository storageObjectRepository,  IStorageService storageService)
    {
        _storageObjectRepository = storageObjectRepository;
        _storageService = storageService;
    }
    
    public async Task<BaseResponse<List<StorageObjectResponseDTO>>> Handle(GetStorageObjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _storageObjectRepository.GetAllByShopId(request.shopId);

            foreach (var obj in response)
            {
                if (!string.IsNullOrEmpty(obj.Key))
                    obj.Key = _storageService.GeneratePresignedUrl(obj.Key, 60);
            }

            return BaseResponse<List<StorageObjectResponseDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<StorageObjectResponseDTO>>.Fail(ex.Message);
        }
    }
}
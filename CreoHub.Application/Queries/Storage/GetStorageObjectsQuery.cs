using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Storage;

public record GetStorageObjectsQuery(Guid shopId) : IRequest<BaseResponse<List<StorageObjectResponseDTO>>>;

public class GetStorageObjectHandler : IRequestHandler<GetStorageObjectsQuery, BaseResponse<List<StorageObjectResponseDTO>>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;

    public GetStorageObjectHandler(IStorageObjectRepository storageObjectRepository)
    {
        _storageObjectRepository = storageObjectRepository;
    }
    
    public async Task<BaseResponse<List<StorageObjectResponseDTO>>> Handle(GetStorageObjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _storageObjectRepository.GetAllByShopId(request.shopId);
            
            return BaseResponse<List<StorageObjectResponseDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<StorageObjectResponseDTO>>.Fail(ex.Message);
        }
    }
}
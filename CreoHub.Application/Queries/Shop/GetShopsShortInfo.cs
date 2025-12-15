using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetShopsShortInfoQuery : IRequest<BaseResponse<List<ShopShortInfoDTO>>>
{
    
}

public class GetShopsShortInfoHandler : IRequestHandler<GetShopsShortInfoQuery, BaseResponse<List<ShopShortInfoDTO>>>
{
    
    private readonly IShopRepository _shopRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetShopsShortInfoHandler(IShopRepository shopRepository, IUnitOfWork unitOfWork)
    {
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BaseResponse<List<ShopShortInfoDTO>>> Handle(GetShopsShortInfoQuery request, CancellationToken cancellationToken)
    {
        try
        { 
            var data = await _shopRepository.GetShopsShortInfoAsync();
            return BaseResponse<List<ShopShortInfoDTO>>.Success(data);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ShopShortInfoDTO>>.Fail(ex.Message);
        }
        
    }
}
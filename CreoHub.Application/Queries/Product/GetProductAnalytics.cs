using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductAnalyticsQuery(Guid shopId, int productId) : IRequest<BaseResponse<ProductAnalyticsDTO>>;

public class GetProductAnalyticsHandler : IRequestHandler<GetProductAnalyticsQuery, BaseResponse<ProductAnalyticsDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService _storageService;
    
    public GetProductAnalyticsHandler(IProductRepository productRepository,  IStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService = storageService;
    }
    
    public async Task<BaseResponse<ProductAnalyticsDTO>> Handle(GetProductAnalyticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            ProductAnalyticsDTO response = await _productRepository.GetProductAnalyticsById(request.productId);
            if (response.ShopId != request.shopId)
            {
                throw new Exception("Shop id does not match shop id " + request.shopId);
            }
            
            return  BaseResponse<ProductAnalyticsDTO>.Success(response);
        }
        catch(Exception ex)
        {
            return BaseResponse<ProductAnalyticsDTO>.Fail(ex.Message);
        }
    }
}
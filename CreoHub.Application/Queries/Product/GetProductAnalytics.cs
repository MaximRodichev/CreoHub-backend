using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductAnalyticsQuery(Guid shopId, int productId) : IRequest<BaseResponse<ProductAnalyticsDTO>>;

public class GetProductAnalyticsHandler : IRequestHandler<GetProductAnalyticsQuery, BaseResponse<ProductAnalyticsDTO>>
{
    IProductRepository _productRepository;

    public GetProductAnalyticsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
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
            else
            {
                return  BaseResponse<ProductAnalyticsDTO>.Success(response);
            }
        }
        catch(Exception ex)
        {
            return BaseResponse<ProductAnalyticsDTO>.Fail(ex.Message);
        }
    }
}
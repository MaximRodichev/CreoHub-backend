using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsByShopQuery(Guid shopId) : IRequest<BaseResponse<IReadOnlyList<ProductInfoDTO>>>;


public class GetProductsByShopHandler : IRequestHandler<GetProductsByShopQuery, BaseResponse<IReadOnlyList<ProductInfoDTO>>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsByShopHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    
    public async Task<BaseResponse<IReadOnlyList<ProductInfoDTO>>> Handle(GetProductsByShopQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ProductInfoDTO> result = await _productRepository.GetProductsInfoByShopId(request.shopId);
            return BaseResponse<IReadOnlyList<ProductInfoDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<IReadOnlyList<ProductInfoDTO>>.Fail(ex.Message);
        }
    }
}
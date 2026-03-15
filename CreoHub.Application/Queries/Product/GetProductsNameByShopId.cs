using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsNameByShopIdQuery(Guid shopId) : IRequest<BaseResponse<List<ProductShortInfoDTO>>>;

public class GetProductsNameByShopIdHandler : IRequestHandler<GetProductsNameByShopIdQuery, BaseResponse<List<ProductShortInfoDTO>>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsNameByShopIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<List<ProductShortInfoDTO>>> Handle(GetProductsNameByShopIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productRepository.GetProductsNamesByShopId(request.shopId);
            
            return BaseResponse<List<ProductShortInfoDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ProductShortInfoDTO>>.Fail(ex.Message);
        }
    }
}
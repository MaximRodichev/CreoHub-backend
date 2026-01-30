using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsNameByShopIdQuery(Guid shopId) : IRequest<BaseResponse<List<ProductNameDTO>>>;

public class GetProductsNameByShopIdHandler : IRequestHandler<GetProductsNameByShopIdQuery, BaseResponse<List<ProductNameDTO>>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsNameByShopIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<List<ProductNameDTO>>> Handle(GetProductsNameByShopIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productRepository.GetProductsNamesByShopId(request.shopId);
            
            return BaseResponse<List<ProductNameDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ProductNameDTO>>.Fail(ex.Message);
        }
    }
}
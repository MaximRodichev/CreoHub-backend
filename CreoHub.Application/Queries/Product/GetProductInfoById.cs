using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductInfoByNameQuery(string name):IRequest<BaseResponse<ProductInfoDTO>>{}

public class GetProductInfoByNameHandler : IRequestHandler<GetProductInfoByNameQuery, BaseResponse<ProductInfoDTO>>
{
    private readonly IProductRepository _productRepository;

    public GetProductInfoByNameHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<ProductInfoDTO>> Handle(GetProductInfoByNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            ProductInfoDTO productInfoDto = await _productRepository.GetProductByName(request.name);
            return BaseResponse<ProductInfoDTO>.Success(productInfoDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductInfoDTO>.Fail(ex.Message);
        }
    }
}
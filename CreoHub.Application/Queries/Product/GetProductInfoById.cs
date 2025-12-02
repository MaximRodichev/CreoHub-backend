using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductInfoByIdQuery(int Id):IRequest<BaseResponse<ProductInfoDTO>>{}

public class GetProductInfoByIdHandler : IRequestHandler<GetProductInfoByIdQuery, BaseResponse<ProductInfoDTO>>
{
    private readonly IProductRepository _productRepository;

    public GetProductInfoByIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<ProductInfoDTO>> Handle(GetProductInfoByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            ProductInfoDTO productInfoDto = await _productRepository.GetProductInfoById(request.Id);
            return BaseResponse<ProductInfoDTO>.Success(productInfoDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductInfoDTO>.Fail(ex.Message);
        }
    }
}
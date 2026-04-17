using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductContentFilesQuery(int ProductId)
    : IRequest<BaseResponse<List<ProductContentFileDTO>>>;

public class GetProductContentFilesHandler
    : IRequestHandler<GetProductContentFilesQuery, BaseResponse<List<ProductContentFileDTO>>>
{
    private readonly IProductRepository _productRepository;

    public GetProductContentFilesHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<BaseResponse<List<ProductContentFileDTO>>> Handle(
        GetProductContentFilesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var products = await _productRepository.GetProductsByIds([request.ProductId]);
            var product = products.FirstOrDefault();

            if (product is null)
                return BaseResponse<List<ProductContentFileDTO>>.Fail("Product not found.");

            var result = product.ContentFiles
                .Select(file => new ProductContentFileDTO
                {
                    FileId      = file.Id,
                    FileName    = file.PreviewName,
                    PriceWeight = file.PriceWeight,
                })
                .ToList();

            return BaseResponse<List<ProductContentFileDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ProductContentFileDTO>>.Fail(ex.Message);
        }
    }
}

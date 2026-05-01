using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsExtendViewsQuery(Guid shopId) : IRequest<BaseResponse<List<ProductViewExtendedDTO>>>;

public class  GetProductsExtendViewsHandler : IRequestHandler<GetProductsExtendViewsQuery, BaseResponse<List<ProductViewExtendedDTO>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService _storageService;

    public GetProductsExtendViewsHandler(IProductRepository productRepository, IStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService = storageService;
    }
    
    public async Task<BaseResponse<List<ProductViewExtendedDTO>>> Handle(GetProductsExtendViewsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productRepository.GetProductsExtendedInfo(request.shopId);

            foreach (var p in response)
            {
                if (!string.IsNullOrEmpty(p.PreviewKey))
                    p.PreviewKey = _storageService.GeneratePresignedUrl(p.PreviewKey, 60);
                if (!string.IsNullOrEmpty(p.PreviewThumbnailKey))
                    p.PreviewThumbnailKey = _storageService.GeneratePresignedUrl(p.PreviewThumbnailKey, 60);
            }

            return BaseResponse<List<ProductViewExtendedDTO>>.Success(response);
        }
        catch(Exception ex)
        {
            return BaseResponse<List<ProductViewExtendedDTO>>.Fail(ex.Message);
        }
    }
}
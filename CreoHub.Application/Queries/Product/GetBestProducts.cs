using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Product;

/// <summary>
/// Возвращает два списка по 6 продуктов: новинки и популярные.
/// Используется на главной странице магазина.
/// </summary>
public record GetBestProductsQuery : IRequest<BaseResponse<BestProductsDTO>>;

public class GetBestProductsHandler : IRequestHandler<GetBestProductsQuery, BaseResponse<BestProductsDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService _storageService;

    public GetBestProductsHandler(IProductRepository productRepository, IStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService = storageService;
    }

    private void SignProducts(IEnumerable<ProductViewDTO> products)
    {
        foreach (var p in products)
        {
            if (!string.IsNullOrEmpty(p.PreviewKey))
                p.PreviewKey = _storageService.GeneratePresignedUrl(p.PreviewKey, 60);
            if (!string.IsNullOrEmpty(p.PreviewThumbnailKey))
                p.PreviewThumbnailKey = _storageService.GeneratePresignedUrl(p.PreviewThumbnailKey, 60);
        }
    }

    public async Task<BaseResponse<BestProductsDTO>> Handle(
        GetBestProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (newest, _) = await _productRepository.GetProductsByFilters(new FiltersDto
            {
                Page     = 1,
                PageSize = 6,
                SortOrder = SortOrder.Latests
            });

            var (popular, _) = await _productRepository.GetProductsByFilters(new FiltersDto
            {
                Page     = 1,
                PageSize = 6,
                SortOrder = SortOrder.Popularity
            });

            SignProducts(newest);
            SignProducts(popular);

            return BaseResponse<BestProductsDTO>.Success(new BestProductsDTO
            {
                Newest  = newest.ToList(),
                Popular = popular.ToList()
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<BestProductsDTO>.Fail(ex.Message);
        }
    }
}

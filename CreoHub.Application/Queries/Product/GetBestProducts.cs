using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
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

    public GetBestProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
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

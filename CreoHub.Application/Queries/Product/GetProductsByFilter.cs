using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductsByFilterQuery(FiltersDto filters) : IRequest<BaseResponse<PageViewDTO>>
{
    
}

public class GetProductsByFilterHandler : IRequestHandler<GetProductsByFilterQuery, BaseResponse<PageViewDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService _storageService;

    public GetProductsByFilterHandler(IProductRepository ProductRepository, IStorageService StorageService)
    {
        _productRepository = ProductRepository;
        _storageService = StorageService;
    }
    
    public async Task<BaseResponse<PageViewDTO>> Handle(GetProductsByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            (IReadOnlyList<ProductViewDTO> products, int count) = await _productRepository.GetProductsByFilters(request.filters);

            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(p.PreviewKey))
                    p.PreviewKey = _storageService.GeneratePresignedUrl(p.PreviewKey, 60);
                if (!string.IsNullOrEmpty(p.PreviewThumbnailKey))
                    p.PreviewThumbnailKey = _storageService.GeneratePresignedUrl(p.PreviewThumbnailKey, 60);
            }

            PageViewDTO page = new PageViewDTO()
            {
                Products = products,
                CountProducts = count
            };
            return BaseResponse<PageViewDTO>.Success(page);
        }
        catch (Exception ex)
        {
            return BaseResponse<PageViewDTO>.Fail(ex.Message);
        }
    }
}
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductInfoByNameQuery(string name) : IRequest<BaseResponse<ProductInfoDTO>> {}

public class GetProductInfoByNameHandler : IRequestHandler<GetProductInfoByNameQuery, BaseResponse<ProductInfoDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService _storageService;

    public GetProductInfoByNameHandler(IProductRepository productRepository, IStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService = storageService;
    }

    public async Task<BaseResponse<ProductInfoDTO>> Handle(GetProductInfoByNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var productInfoDto = await _productRepository.GetProductByName(request.name);
            if (productInfoDto == null) return BaseResponse<ProductInfoDTO>.Fail("Not found");
            SignMediaUrls(productInfoDto, _storageService);
            return BaseResponse<ProductInfoDTO>.Success(productInfoDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductInfoDTO>.Fail(ex.Message);
        }
    }

    internal static void SignMediaUrls(ProductInfoDTO dto, IStorageService storageService)
    {
        foreach (var m in dto.MediaViews)
        {
            m.Key = storageService.GeneratePresignedUrl(m.Key, 60);
            if (!string.IsNullOrEmpty(m.ThumbnailKey))
                m.ThumbnailKey = storageService.GeneratePresignedUrl(m.ThumbnailKey, 60);
        }
    }
}

// ── By numeric ID ─────────────────────────────────────────────────────────────

public record GetProductInfoByIdQuery(int Id) : IRequest<BaseResponse<ProductInfoDTO>> {}

public class GetProductInfoByIdHandler : IRequestHandler<GetProductInfoByIdQuery, BaseResponse<ProductInfoDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService _storageService;

    public GetProductInfoByIdHandler(IProductRepository productRepository, IStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService = storageService;
    }

    public async Task<BaseResponse<ProductInfoDTO>> Handle(GetProductInfoByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var productInfoDto = await _productRepository.GetProductInfoById(request.Id);
            if (productInfoDto == null) return BaseResponse<ProductInfoDTO>.Fail("Not found");
            GetProductInfoByNameHandler.SignMediaUrls(productInfoDto, _storageService);
            return BaseResponse<ProductInfoDTO>.Success(productInfoDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductInfoDTO>.Fail(ex.Message);
        }
    }
}
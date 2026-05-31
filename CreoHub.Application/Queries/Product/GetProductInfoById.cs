using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

// X-Session-Id is optionally forwarded by the frontend for anonymous tracking.

namespace CreoHub.Application.Queries.Product;

public record GetProductInfoByNameQuery(
    string  name,
    Guid?   UserId    = null,
    string? SessionId = null
) : IRequest<BaseResponse<ProductInfoDTO>>;

public class GetProductInfoByNameHandler : IRequestHandler<GetProductInfoByNameQuery, BaseResponse<ProductInfoDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService    _storageService;
    private readonly IEventTracker      _events;

    public GetProductInfoByNameHandler(
        IProductRepository productRepository,
        IStorageService    storageService,
        IEventTracker      events)
    {
        _productRepository = productRepository;
        _storageService    = storageService;
        _events            = events;
    }

    public async Task<BaseResponse<ProductInfoDTO>> Handle(GetProductInfoByNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var productInfoDto = await _productRepository.GetProductByName(request.name);
            if (productInfoDto == null) return BaseResponse<ProductInfoDTO>.Fail("Not found");
            SignMediaUrls(productInfoDto, _storageService);

            _events.Track(EventTypes.ProductView,
                productId: productInfoDto.Id,
                userId:    request.UserId,
                sessionId: request.SessionId);

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

public record GetProductInfoByIdQuery(
    int     Id,
    Guid?   UserId    = null,
    string? SessionId = null
) : IRequest<BaseResponse<ProductInfoDTO>>;

public class GetProductInfoByIdHandler : IRequestHandler<GetProductInfoByIdQuery, BaseResponse<ProductInfoDTO>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService    _storageService;
    private readonly IEventTracker      _events;

    public GetProductInfoByIdHandler(
        IProductRepository productRepository,
        IStorageService    storageService,
        IEventTracker      events)
    {
        _productRepository = productRepository;
        _storageService    = storageService;
        _events            = events;
    }

    public async Task<BaseResponse<ProductInfoDTO>> Handle(GetProductInfoByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var productInfoDto = await _productRepository.GetProductInfoById(request.Id);
            if (productInfoDto == null) return BaseResponse<ProductInfoDTO>.Fail("Not found");
            GetProductInfoByNameHandler.SignMediaUrls(productInfoDto, _storageService);

            _events.Track(EventTypes.ProductView,
                productId: request.Id,
                userId:    request.UserId,
                sessionId: request.SessionId);

            return BaseResponse<ProductInfoDTO>.Success(productInfoDto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductInfoDTO>.Fail(ex.Message);
        }
    }
}
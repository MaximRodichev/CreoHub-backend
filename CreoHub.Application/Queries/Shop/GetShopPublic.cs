using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetShopPublicQuery(string Name) : IRequest<BaseResponse<ShopPublicDTO>>;

public class GetShopPublicHandler : IRequestHandler<GetShopPublicQuery, BaseResponse<ShopPublicDTO>>
{
    private readonly IShopRepository _shopRepository;
    private readonly IStorageService _storageService;

    public GetShopPublicHandler(IShopRepository shopRepository, IStorageService storageService)
    {
        _shopRepository = shopRepository;
        _storageService = storageService;
    }

    public async Task<BaseResponse<ShopPublicDTO>> Handle(GetShopPublicQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _shopRepository.GetShopPublicAsync(request.Name);
            if (dto is null)
                return BaseResponse<ShopPublicDTO>.Fail("Магазин не найден");

            // Sign all R2 URLs
            if (!string.IsNullOrEmpty(dto.BannerKey))
                dto.BannerKey = _storageService.GeneratePresignedUrl(dto.BannerKey, 60);
            if (!string.IsNullOrEmpty(dto.LogoKey))
                dto.LogoKey = _storageService.GeneratePresignedUrl(dto.LogoKey, 60);

            foreach (var p in dto.Products)
            {
                if (!string.IsNullOrEmpty(p.PreviewKey))
                    p.PreviewKey = _storageService.GeneratePresignedUrl(p.PreviewKey, 60);
                if (!string.IsNullOrEmpty(p.PreviewThumbnailKey))
                    p.PreviewThumbnailKey = _storageService.GeneratePresignedUrl(p.PreviewThumbnailKey, 60);
            }

            return BaseResponse<ShopPublicDTO>.Success(dto);
        }
        catch (Exception ex)
        {
            return BaseResponse<ShopPublicDTO>.Fail(ex.Message);
        }
    }
}

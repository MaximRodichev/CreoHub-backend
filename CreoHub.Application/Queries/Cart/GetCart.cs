using CreoHub.Application.DTO;
using CreoHub.Application.DTO.CartDTOs;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Cart;

public record GetCartQuery(Guid UserId) : IRequest<BaseResponse<List<CartItemDTO>>>;

public class GetCartHandler : IRequestHandler<GetCartQuery, BaseResponse<List<CartItemDTO>>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IMediaProductRepository _mediaProductRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IProductRepository _productRepository;

    public GetCartHandler(
        ICartRepository cartRepository,
        IMediaProductRepository mediaProductRepository,
        IContentFileRepository contentFileRepository,
        IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _mediaProductRepository = mediaProductRepository;
        _contentFileRepository = contentFileRepository;
        _productRepository = productRepository;
    }

    public async Task<BaseResponse<List<CartItemDTO>>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cart = await _cartRepository.GetFullCartAsync(request.UserId);

            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

            var allContentFiles  = await _contentFileRepository.GetByProductIdsAsync(productIds);
            var allPreviewKeys   = await _mediaProductRepository.GetPreviewKeysWithThumbnailByProductIds(productIds);
            var allProducts      = await _productRepository.GetProductsByIds(productIds);
            var productMap       = allProducts.ToDictionary(p => p.Id);

            var response = cart.Items.Select(item =>
            {
                productMap.TryGetValue(item.ProductId, out var product);
                allPreviewKeys.TryGetValue(item.ProductId, out var keys);

                var latestPrice = product?.Prices
                    .OrderByDescending(p => p.Date)
                    .Select(p => p.Value)
                    .FirstOrDefault() ?? 0m;

                return new CartItemDTO
                {
                    CartItemId          = item.Id,
                    ProductId           = item.ProductId,
                    Name                = product?.Name ?? $"Товар #{item.ProductId}",
                    Price               = latestPrice,
                    ContentFileInfos    = allContentFiles
                        .Where(cf => cf.ProductId == item.ProductId)
                        .Select(x => new ContentFileInfo
                        {
                            Id          = x.Id,
                            PreviewName = x.PreviewName,
                            PriceWeight = x.PriceWeight
                        }).ToList(),
                    SelectedContentItems = item.SelectedFiles.Select(f => f.ContentFileId).ToList(),
                    PreviewKey           = keys.Key,
                    PreviewThumbnailKey  = keys.ThumbnailKey,
                };
            }).ToList();

            return BaseResponse<List<CartItemDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<CartItemDTO>>.Fail(ex.Message);
        }
    }
}
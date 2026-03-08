using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetShopDashboardQuery(Guid shopId, DateTime? from = null, DateTime? to = null) : IRequest<BaseResponse<ShopDashboardDTO>>;

public class GetShopDashboardHandler : IRequestHandler<GetShopDashboardQuery, BaseResponse<ShopDashboardDTO>>
{
    public IShopRepository _shopRepository { get; init; }
    public IOrderRepository _orderRepository { get; init; }
    public IProductRepository _productRepository { get; init; }
    public ITagRepository _tagRepository { get; init; }

    public GetShopDashboardHandler(ITagRepository tagRepository, IShopRepository shopRepository,  IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _shopRepository = shopRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _tagRepository = tagRepository;
    }
    
    public async Task<BaseResponse<ShopDashboardDTO>> Handle(GetShopDashboardQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var topProducts =
                await _productRepository.GetProductsStatsByShopIdAsync(request.shopId, request.from, request.to,
                    limit: 5);
            var stats = await _shopRepository.GetShopStatsAsync(request.shopId, request.from, request.to);
            var lastDeals = await _orderRepository.GetOrdersShortInfoByShopIdAsync(request.shopId, from: request.from, to: request.to, limit: 5);
            var tagStats = await _tagRepository.GetTagStatsByShopAsync(request.shopId, request.from, request.to, limit: 5);
            var response = new ShopDashboardDTO()
            {
                TagAnalytics = tagStats,
                RecentDeals = lastDeals,
                Stats = stats,
                TopProducts = topProducts
            };
            return BaseResponse<ShopDashboardDTO>.Success(response);
        }
        catch(Exception ex)
        {
            return BaseResponse<ShopDashboardDTO>.Fail(ex.Message);
        }
    }
}

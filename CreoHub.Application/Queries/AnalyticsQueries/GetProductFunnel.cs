using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.AnalyticsQueries;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetProductFunnelQuery(
    int    ProductId,
    Guid   ShopId,
    int    Days = 30
) : IRequest<BaseResponse<ProductFunnelDTO>>;

// ── DTO ───────────────────────────────────────────────────────────────────────

public record ProductFunnelDTO(
    int     ProductId,
    int     Days,
    int     Views,
    int     CartAdds,
    int     Purchases,
    decimal ConversionRate   // purchases / views × 100, %
);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetProductFunnelHandler
    : IRequestHandler<GetProductFunnelQuery, BaseResponse<ProductFunnelDTO>>
{
    private readonly IUserEventRepository _events;
    private readonly IProductRepository   _products;

    public GetProductFunnelHandler(
        IUserEventRepository events,
        IProductRepository   products)
    {
        _events   = events;
        _products = products;
    }

    public async Task<BaseResponse<ProductFunnelDTO>> Handle(
        GetProductFunnelQuery request, CancellationToken ct)
    {
        try
        {
            // Verify product belongs to the shop
            var product = await _products.GetByIdAsync(request.ProductId);
            if (product is null || product.OwnerId != request.ShopId)
                return BaseResponse<ProductFunnelDTO>.Fail("Продукт не найден или не принадлежит этому магазину.");

            var to   = DateTime.UtcNow;
            var from = to.AddDays(-request.Days);

            var counts = await _events.GetCountsByTypeForProductAsync(request.ProductId, from, to, ct);

            int Get(string k) => counts.TryGetValue(k, out var v) ? v : 0;

            var views    = Get(EventTypes.ProductView);
            var cartAdds = Get(EventTypes.CartAdd);
            var buys     = Get(EventTypes.ProductPurchased);

            var conversion = views > 0
                ? Math.Round(buys / (decimal)views * 100, 2)
                : 0m;

            return BaseResponse<ProductFunnelDTO>.Success(new ProductFunnelDTO(
                request.ProductId,
                request.Days,
                views,
                cartAdds,
                buys,
                conversion));
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductFunnelDTO>.Fail(ex.Message);
        }
    }
}

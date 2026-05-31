using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.AnalyticsQueries;

// ── Query ─────────────────────────────────────────────────────────────────────

public record GetShopAnalyticsQuery(Guid ShopId, int Days = 30)
    : IRequest<BaseResponse<ShopAnalyticsDTO>>;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ShopAnalyticsDTO(
    int  Days,
    int  TotalViews,
    int  TotalCartAdds,
    int  TotalPurchases,
    IReadOnlyList<ProductAnalyticsRow> Products);

public record ProductAnalyticsRow(
    int    ProductId,
    string ProductName,
    int    Views,
    int    CartAdds,
    int    Purchases,
    decimal ConversionRate);

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetShopAnalyticsHandler
    : IRequestHandler<GetShopAnalyticsQuery, BaseResponse<ShopAnalyticsDTO>>
{
    private readonly IUserEventRepository _events;

    public GetShopAnalyticsHandler(IUserEventRepository events) => _events = events;

    public async Task<BaseResponse<ShopAnalyticsDTO>> Handle(
        GetShopAnalyticsQuery request, CancellationToken ct)
    {
        try
        {
            var to   = DateTime.UtcNow;
            var from = to.AddDays(-request.Days);

            var stats = await _events.GetProductStatsForShopAsync(request.ShopId, from, to, ct);

            var rows = stats
                .OrderByDescending(s => s.Views)
                .Select(s => new ProductAnalyticsRow(
                    s.ProductId,
                    s.ProductName,
                    s.Views,
                    s.CartAdds,
                    s.Purchases,
                    s.Views > 0
                        ? Math.Round(s.Purchases / (decimal)s.Views * 100, 2)
                        : 0m))
                .ToList();

            return BaseResponse<ShopAnalyticsDTO>.Success(new ShopAnalyticsDTO(
                request.Days,
                TotalViews:     rows.Sum(r => r.Views),
                TotalCartAdds:  rows.Sum(r => r.CartAdds),
                TotalPurchases: rows.Sum(r => r.Purchases),
                Products:       rows));
        }
        catch (Exception ex)
        {
            return BaseResponse<ShopAnalyticsDTO>.Fail(ex.Message);
        }
    }
}

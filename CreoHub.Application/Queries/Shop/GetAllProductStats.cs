using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

/// <summary>
/// Returns revenue and order count for every product owned by the shop,
/// derived from actual completed orders.
/// days = 0 → all-time (no date filter).
/// days > 0 → last N days.
/// </summary>
public record GetAllProductStatsQuery(Guid ShopId, int Days = 0)
    : IRequest<BaseResponse<IReadOnlyList<ProductStatsDTO>>>;

public class GetAllProductStatsHandler
    : IRequestHandler<GetAllProductStatsQuery, BaseResponse<IReadOnlyList<ProductStatsDTO>>>
{
    private readonly IProductRepository _products;

    public GetAllProductStatsHandler(IProductRepository products) => _products = products;

    public async Task<BaseResponse<IReadOnlyList<ProductStatsDTO>>> Handle(
        GetAllProductStatsQuery request, CancellationToken ct)
    {
        try
        {
            // days = 0 means no date filter (all-time)
            DateTime? from = request.Days > 0 ? DateTime.UtcNow.AddDays(-request.Days) : null;
            DateTime? to   = request.Days > 0 ? DateTime.UtcNow                        : null;

            // limit: null → all products, not just top-N
            var stats = await _products.GetProductsStatsByShopIdAsync(
                request.ShopId, from, to, limit: null);

            return BaseResponse<IReadOnlyList<ProductStatsDTO>>.Success(stats);
        }
        catch (Exception ex)
        {
            return BaseResponse<IReadOnlyList<ProductStatsDTO>>.Fail(ex.Message);
        }
    }
}

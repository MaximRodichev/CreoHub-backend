using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetShopBalanceQuery(Guid ShopId) : IRequest<BaseResponse<ShopBalanceDTO>>;

public class ShopBalanceDTO
{
    public decimal AvailableAmount { get; init; }
    public decimal PendingAmount   { get; init; }
}

public class GetShopBalanceHandler : IRequestHandler<GetShopBalanceQuery, BaseResponse<ShopBalanceDTO>>
{
    private readonly IShopBalanceRepository _shopBalanceRepository;

    public GetShopBalanceHandler(IShopBalanceRepository shopBalanceRepository)
    {
        _shopBalanceRepository = shopBalanceRepository;
    }

    public async Task<BaseResponse<ShopBalanceDTO>> Handle(
        GetShopBalanceQuery request, CancellationToken cancellationToken)
    {
        var balance = await _shopBalanceRepository.GetByShopIdAsync(request.ShopId);

        // Магазин мог ещё ничего не продать — возвращаем нули
        return BaseResponse<ShopBalanceDTO>.Success(new ShopBalanceDTO
        {
            AvailableAmount = balance?.AvailableAmount ?? 0m,
            PendingAmount   = balance?.PendingAmount   ?? 0m,
        });
    }
}

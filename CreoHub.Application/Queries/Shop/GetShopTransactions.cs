using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetShopTransactionsQuery(Guid ShopId) : IRequest<BaseResponse<List<ShopTransactionDTO>>>;

public class ShopTransactionDTO
{
    public Guid   Id                  { get; init; }
    public string TrackId             { get; init; } = "";
    public string TransactionType     { get; init; } = "";
    public string TransactionStatus   { get; init; } = "";
    public decimal FullAmount         { get; init; }
    public decimal NetAmount          { get; init; }
    public decimal PlatformFeeAmount  { get; init; }
    public decimal PlatformFeePercent { get; init; }
    public DateTime CreatedAt         { get; init; }
    public DateTime? PaidAt           { get; init; }
}

public class GetShopTransactionsHandler
    : IRequestHandler<GetShopTransactionsQuery, BaseResponse<List<ShopTransactionDTO>>>
{
    private readonly IShopTransactionRepository _shopTransactionRepository;

    public GetShopTransactionsHandler(IShopTransactionRepository shopTransactionRepository)
    {
        _shopTransactionRepository = shopTransactionRepository;
    }

    public async Task<BaseResponse<List<ShopTransactionDTO>>> Handle(
        GetShopTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _shopTransactionRepository.GetByShopIdAsync(request.ShopId);

        var dtos = transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ShopTransactionDTO
            {
                Id                  = t.Id,
                TrackId             = t.TrackId,
                TransactionType     = t.TransactionType.ToString(),
                TransactionStatus   = t.TransactionStatus.ToString(),
                FullAmount          = t.FullAmount,
                NetAmount           = t.NetAmount,
                PlatformFeeAmount   = t.PlatformFeeAmount,
                PlatformFeePercent  = t.PlatformFeePercent,
                CreatedAt           = t.CreatedAt,
                PaidAt              = t.PaidAt,
            })
            .ToList();

        return BaseResponse<List<ShopTransactionDTO>>.Success(dtos);
    }
}

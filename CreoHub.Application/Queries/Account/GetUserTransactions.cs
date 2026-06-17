using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;
using System.Text.Json.Serialization;

namespace CreoHub.Application.Queries.Account;

public record GetUserTransactionsQuery(Guid UserId, int Page = 0, int PageSize = 50)
    : IRequest<BaseResponse<List<UserTransactionDTO>>>;

public class GetUserTransactionsHandler
    : IRequestHandler<GetUserTransactionsQuery, BaseResponse<List<UserTransactionDTO>>>
{
    private readonly IUserTransactionRepository _repo;

    public GetUserTransactionsHandler(IUserTransactionRepository repo)
    {
        _repo = repo;
    }

    public async Task<BaseResponse<List<UserTransactionDTO>>> Handle(
        GetUserTransactionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var all = await _repo.GetByUserIdAsync(request.UserId);

            var result = all
                .OrderByDescending(t => t.CreatedAt)
                .Skip(request.Page * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new UserTransactionDTO
                {
                    Id            = t.Id,
                    FullAmount    = t.FullAmount,
                    PaidAmount    = t.PaidAmount,
                    Type          = t.TransactionType,
                    Status        = t.TransactionStatus,
                    CreatedAt     = t.CreatedAt,
                    PaidAt        = t.PaidAt,
                    TxHash        = t.TxHash,
                    SenderAddress = t.SenderAddress,
                })
                .ToList();

            return BaseResponse<List<UserTransactionDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<UserTransactionDTO>>.Fail(ex.Message);
        }
    }
}

public class UserTransactionDTO
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public decimal FullAmount { get; set; }
    public decimal? PaidAmount { get; set; }

    [JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TransactionType Type { get; set; }

    [JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public TransactionStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? TxHash { get; set; }
    public string? SenderAddress { get; set; }
}

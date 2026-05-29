using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record ProductStatusLogEntryDTO(
    int           Id,
    string        OldStatus,
    string        NewStatus,
    string?       Reason,
    Guid?         ChangedById,
    DateTime      CreatedAt);

public record GetProductStatusLogQuery(int ProductId)
    : IRequest<BaseResponse<List<ProductStatusLogEntryDTO>>>;

public class GetProductStatusLogHandler
    : IRequestHandler<GetProductStatusLogQuery, BaseResponse<List<ProductStatusLogEntryDTO>>>
{
    private readonly IProductStatusLogRepository _repo;

    public GetProductStatusLogHandler(IProductStatusLogRepository repo) => _repo = repo;

    public async Task<BaseResponse<List<ProductStatusLogEntryDTO>>> Handle(
        GetProductStatusLogQuery request, CancellationToken ct)
    {
        var logs = await _repo.GetByProductIdAsync(request.ProductId, ct);
        var result = logs.Select(l => new ProductStatusLogEntryDTO(
            l.Id,
            l.OldStatus.ToString(),
            l.NewStatus.ToString(),
            l.Reason,
            l.ChangedById,
            l.CreatedAt)).ToList();

        return BaseResponse<List<ProductStatusLogEntryDTO>>.Success(result);
    }
}

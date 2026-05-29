using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record ProductEditHistoryDTO(
    int      Id,
    Guid?    EditorId,
    DateTime CreatedAt,
    string   SnapshotJson);

public record GetProductEditHistoryQuery(int ProductId)
    : IRequest<BaseResponse<List<ProductEditHistoryDTO>>>;

public class GetProductEditHistoryHandler
    : IRequestHandler<GetProductEditHistoryQuery, BaseResponse<List<ProductEditHistoryDTO>>>
{
    private readonly IProductEditHistoryRepository _repo;

    public GetProductEditHistoryHandler(IProductEditHistoryRepository repo)
        => _repo = repo;

    public async Task<BaseResponse<List<ProductEditHistoryDTO>>> Handle(
        GetProductEditHistoryQuery request, CancellationToken ct)
    {
        try
        {
            var entries = await _repo.GetByProductIdAsync(request.ProductId, limit: 10, ct: ct);
            var dtos = entries.Select(e => new ProductEditHistoryDTO(
                e.Id, e.EditorId, e.CreatedAt, e.SnapshotJson)).ToList();
            return BaseResponse<List<ProductEditHistoryDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ProductEditHistoryDTO>>.Fail(ex.Message);
        }
    }
}

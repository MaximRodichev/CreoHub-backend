using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record GetBroadcastJobsQuery() : IRequest<BaseResponse<List<BroadcastJobDTO>>>;

public record BroadcastJobDTO(
    int      Id,
    string   Message,
    string   Status,
    Guid     CreatedBy,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int      TotalUsers,
    int      SentCount,
    int      FailedCount,
    string?  ErrorLog);

public class GetBroadcastJobsHandler
    : IRequestHandler<GetBroadcastJobsQuery, BaseResponse<List<BroadcastJobDTO>>>
{
    private readonly IBroadcastJobRepository _broadcastRepo;

    public GetBroadcastJobsHandler(IBroadcastJobRepository broadcastRepo) =>
        _broadcastRepo = broadcastRepo;

    public async Task<BaseResponse<List<BroadcastJobDTO>>> Handle(
        GetBroadcastJobsQuery request, CancellationToken ct)
    {
        try
        {
            var jobs = await _broadcastRepo.GetAllAsync(ct);
            var dtos = jobs.Select(j => new BroadcastJobDTO(
                j.Id, j.Message, j.Status, j.CreatedBy,
                j.CreatedAt, j.StartedAt, j.CompletedAt,
                j.TotalUsers, j.SentCount, j.FailedCount, j.ErrorLog))
                .ToList();
            return BaseResponse<List<BroadcastJobDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<BroadcastJobDTO>>.Fail(ex.Message);
        }
    }
}

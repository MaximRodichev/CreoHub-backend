using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

public record CreateBroadcastJobCommand(string Message, Guid AdminUserId)
    : IRequest<BaseResponse<int>>;

public class CreateBroadcastJobHandler
    : IRequestHandler<CreateBroadcastJobCommand, BaseResponse<int>>
{
    private readonly IBroadcastJobRepository _broadcastRepo;
    private readonly IUnitOfWork             _unitOfWork;

    public CreateBroadcastJobHandler(
        IBroadcastJobRepository broadcastRepo,
        IUnitOfWork             unitOfWork)
    {
        _broadcastRepo = broadcastRepo;
        _unitOfWork    = unitOfWork;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateBroadcastJobCommand request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BaseResponse<int>.Fail("Сообщение не может быть пустым.");

            var job = BroadcastJob.Create(request.Message.Trim(), request.AdminUserId);
            await _broadcastRepo.AddAsync(job, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return BaseResponse<int>.Success(job.Id);
        }
        catch (Exception ex)
        {
            return BaseResponse<int>.Fail(ex.Message);
        }
    }
}

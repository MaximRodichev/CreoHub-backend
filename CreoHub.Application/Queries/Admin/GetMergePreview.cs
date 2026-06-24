using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

/// <summary>Предпросмотр мерджа: оба аккаунта, что переедет, вердикт гардов. Ничего не меняет.</summary>
public record GetMergePreviewQuery(Guid KeepId, Guid MergeId)
    : IRequest<BaseResponse<MergePreviewDto>>;

public class GetMergePreviewHandler : IRequestHandler<GetMergePreviewQuery, BaseResponse<MergePreviewDto>>
{
    private readonly IAdminRepository _admin;

    public GetMergePreviewHandler(IAdminRepository admin) => _admin = admin;

    public async Task<BaseResponse<MergePreviewDto>> Handle(GetMergePreviewQuery request, CancellationToken ct)
    {
        try
        {
            var keep  = await _admin.GetMergeUserAsync(request.KeepId, ct);
            var merge = await _admin.GetMergeUserAsync(request.MergeId, ct);
            if (keep is null)  return BaseResponse<MergePreviewDto>.Fail("Остающийся аккаунт не найден.");
            if (merge is null) return BaseResponse<MergePreviewDto>.Fail("Удаляемый аккаунт не найден.");

            var counts   = await _admin.GetMergeCountsAsync(request.MergeId, ct);
            var blockers = AccountMergeGuards.Evaluate(keep, merge);

            return BaseResponse<MergePreviewDto>.Success(
                new MergePreviewDto(keep, merge, counts, blockers.Count == 0, blockers));
        }
        catch (Exception ex)
        {
            return BaseResponse<MergePreviewDto>.Fail(ex.Message);
        }
    }
}

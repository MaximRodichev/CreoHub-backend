using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record GetAdminUserDetailQuery(Guid UserId) : IRequest<BaseResponse<AdminUserDetailDto>>;

public class GetAdminUserDetailHandler : IRequestHandler<GetAdminUserDetailQuery, BaseResponse<AdminUserDetailDto>>
{
    private readonly IAdminRepository _admin;
    public GetAdminUserDetailHandler(IAdminRepository admin) => _admin = admin;

    public async Task<BaseResponse<AdminUserDetailDto>> Handle(GetAdminUserDetailQuery request, CancellationToken ct)
    {
        try
        {
            var detail = await _admin.GetUserDetailAsync(request.UserId, ct);
            return detail is null
                ? BaseResponse<AdminUserDetailDto>.Fail("Пользователь не найден.")
                : BaseResponse<AdminUserDetailDto>.Success(detail);
        }
        catch (Exception ex)
        {
            return BaseResponse<AdminUserDetailDto>.Fail(ex.Message);
        }
    }
}

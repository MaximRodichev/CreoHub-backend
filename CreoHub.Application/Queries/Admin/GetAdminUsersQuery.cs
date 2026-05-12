using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record GetAdminUsersQuery : IRequest<BaseResponse<List<AdminUserDto>>>;

public class GetAdminUsersHandler : IRequestHandler<GetAdminUsersQuery, BaseResponse<List<AdminUserDto>>>
{
    private readonly IAdminRepository _admin;
    public GetAdminUsersHandler(IAdminRepository admin) => _admin = admin;

    public async Task<BaseResponse<List<AdminUserDto>>> Handle(GetAdminUsersQuery _, CancellationToken ct)
    {
        try
        {
            var users = await _admin.GetAllUsersAsync(ct);
            return BaseResponse<List<AdminUserDto>>.Success(users);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<AdminUserDto>>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record GetAdminShopsQuery : IRequest<BaseResponse<List<AdminShopDto>>>;

public class GetAdminShopsHandler : IRequestHandler<GetAdminShopsQuery, BaseResponse<List<AdminShopDto>>>
{
    private readonly IAdminRepository _admin;
    public GetAdminShopsHandler(IAdminRepository admin) => _admin = admin;

    public async Task<BaseResponse<List<AdminShopDto>>> Handle(GetAdminShopsQuery _, CancellationToken ct)
    {
        try
        {
            var shops = await _admin.GetAllShopsAsync(ct);
            return BaseResponse<List<AdminShopDto>>.Success(shops);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<AdminShopDto>>.Fail(ex.Message);
        }
    }
}

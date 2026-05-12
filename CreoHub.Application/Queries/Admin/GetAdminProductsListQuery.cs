using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record GetAdminProductsListQuery : IRequest<BaseResponse<List<AdminProductNameDto>>>;

public class GetAdminProductsListHandler : IRequestHandler<GetAdminProductsListQuery, BaseResponse<List<AdminProductNameDto>>>
{
    private readonly IAdminRepository _admin;
    public GetAdminProductsListHandler(IAdminRepository admin) => _admin = admin;

    public async Task<BaseResponse<List<AdminProductNameDto>>> Handle(GetAdminProductsListQuery _, CancellationToken ct)
    {
        try
        {
            var products = await _admin.GetAllProductNamesAsync(ct);
            return BaseResponse<List<AdminProductNameDto>>.Success(products);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<AdminProductNameDto>>.Fail(ex.Message);
        }
    }
}

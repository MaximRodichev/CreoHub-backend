using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Tag;

public record GetTagStatsByShopQuery(Guid shopId) :  IRequest<BaseResponse<List<TagStatsDTO>>>;

public class GetTagStatsHandler : IRequestHandler<GetTagStatsByShopQuery, BaseResponse<List<TagStatsDTO>>>
{
    private readonly ITagRepository _tagRepository;

    public GetTagStatsHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository; 
    }
    
    public async  Task<BaseResponse<List<TagStatsDTO>>> Handle(GetTagStatsByShopQuery request, CancellationToken cancellationToken)
    {
        try
        {
            List<TagStatsDTO> result = await _tagRepository.GetTagStatsByShopAsync(request.shopId);
            return BaseResponse<List<TagStatsDTO>>.Success(result);
        }
        catch (Exception e)
        {
            return BaseResponse<List<TagStatsDTO>>.Fail(e.Message);
        }
    }
}
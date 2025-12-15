using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Queries.Tag;

public record GetTagsListCommand() : IRequest<BaseResponse<List<Domain.Entities.Tag>>>;

public class GetTagsListHandler : IRequestHandler<GetTagsListCommand, BaseResponse<List<Domain.Entities.Tag>>>
{
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITagRepository _tagRepository;

    public GetTagsListHandler(IUnitOfWork unitOfWork, ITagRepository tagRepository)
    {
        _unitOfWork = unitOfWork;
        _tagRepository = tagRepository;
    }
    
    public async Task<BaseResponse<List<Domain.Entities.Tag>>> Handle(GetTagsListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _tagRepository.GetAllAsync();
            return BaseResponse<List<Domain.Entities.Tag>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<Domain.Entities.Tag>>.Fail(ex.Message);
        }
    }
}
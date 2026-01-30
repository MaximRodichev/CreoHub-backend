using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;
using CreoHub.Domain.Entities;

namespace CreoHub.Application.Queries.Tag;

public record GetTagsListCommand() : IRequest<BaseResponse<List<string>>>;

public class GetTagsListHandler : IRequestHandler<GetTagsListCommand, BaseResponse<List<string>>>
{
    
    private readonly ITagRepository _tagRepository;
    private readonly IMapper _mapper;

    public GetTagsListHandler(IMapper mapper, ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
        _mapper = mapper;
    }
    
    public async Task<BaseResponse<List<string>>> Handle(GetTagsListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = (await _tagRepository.GetAllAsync()).Select(x=>x.Name).ToList();
            return BaseResponse<List<string>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<string>>.Fail(ex.Message);
        }
    }
}
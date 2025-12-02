using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

public record CreateTagCommand(string name) : IRequest<BaseResponse<bool>> 
{
    
}

public class CreateTagHandler : IRequestHandler<CreateTagCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITagRepository _tagRepository;

    public CreateTagHandler(IUnitOfWork unitOfWork, ITagRepository tagRepository)
    {
        _unitOfWork = unitOfWork;
        _tagRepository = tagRepository;
    }
    
    public async Task<BaseResponse<bool>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tag = new Tag(request.name);
            await _tagRepository.AddAsync(tag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
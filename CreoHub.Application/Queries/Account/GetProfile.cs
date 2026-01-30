using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Account;

public record GetProfileQuery(Guid id) : IRequest<BaseResponse<UserProfileDTO>>
{
    
}

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, BaseResponse<UserProfileDTO>>
{
    private readonly IAccountRepository _accountRepository;

    public GetProfileQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
    
    
    public async Task<BaseResponse<UserProfileDTO>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return BaseResponse<UserProfileDTO>.Success(await _accountRepository.GetUserProfileByUserId(request.id));
        }
        catch (Exception ex)
        {
            return BaseResponse<UserProfileDTO>.Fail(ex.Message);
        }
    }
}
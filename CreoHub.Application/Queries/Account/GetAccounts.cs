using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Account;

public record GetAccountsQuery() : IRequest<BaseResponse<List<User>>>
{
    
}

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, BaseResponse<List<User>>>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountsQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
    
    
    public async Task<BaseResponse<List<User>>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return BaseResponse<List<User>>.Success(await _accountRepository.GetAllAsync());
        }
        catch (Exception ex)
        {
            return BaseResponse<List<User>>.Fail(ex.Message);
        }
    }
}
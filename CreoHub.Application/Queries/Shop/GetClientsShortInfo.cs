using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetClientsShortInfoQuery(Guid shopId) : IRequest<BaseResponse<List<ClientShortInfoDTO>>>
{
    
}

public class GetClientsShortInfoHandler : IRequestHandler<GetClientsShortInfoQuery, BaseResponse<List<ClientShortInfoDTO>>>
{
    
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetClientsShortInfoHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BaseResponse<List<ClientShortInfoDTO>>> Handle(GetClientsShortInfoQuery request, CancellationToken cancellationToken)
    {
        try
        { 
            var data = await _accountRepository.GetClientsShortInfoAsync(request.shopId);
            return BaseResponse<List<ClientShortInfoDTO>>.Success(data);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ClientShortInfoDTO>>.Fail(ex.Message);
        }
        
    }
}
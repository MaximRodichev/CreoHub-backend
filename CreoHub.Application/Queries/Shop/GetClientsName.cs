using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Shop;

public record GetClientsNameQuery(Guid shopId) : IRequest<BaseResponse<List<ClientNameDTO>>>;

public class GetClientsNameHandler : IRequestHandler<GetClientsNameQuery, BaseResponse<List<ClientNameDTO>>>
{   
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public GetClientsNameHandler(IAccountRepository accountRepository,  IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
    }
    
    public async Task<BaseResponse<List<ClientNameDTO>>> Handle(GetClientsNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _accountRepository.GetClientsShortInfoAsync(request.shopId);
            var result = _mapper.Map<List<ClientNameDTO>>(response);
            return BaseResponse<List<ClientNameDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ClientNameDTO>>.Fail(ex.Message);
        }
    }
}
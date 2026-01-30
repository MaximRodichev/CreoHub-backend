using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

public record AuthAccountCommand(AuthAccountDTO dto, DateTime registerDate) :  IRequest<BaseResponse<IdentityDTO>>{}

public class AuthAccountHandler : IRequestHandler<AuthAccountCommand, BaseResponse<IdentityDTO>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuthAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork,  IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper =  mapper;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BaseResponse<IdentityDTO>> Handle(AuthAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _accountRepository.FindUserByCredentials(request.dto.Email, request.dto.TelegramId);
            if (user == null)
            {
                user = User.Create(request.dto.Name, request.dto.Email, request.dto.TelegramId, request.dto.TelegramUsername);
                if (request.registerDate != DateTime.MinValue)
                {
                    user.InjectDate(request.registerDate); //TODO УБРАТЬ НА ПРОДЕ
                }
                await _accountRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }



            var identity = _mapper.Map<IdentityDTO>(user);
            return BaseResponse<IdentityDTO>.Success(identity);
        }
        catch (Exception ex)
        {
            return BaseResponse<IdentityDTO>.Fail(ex.Message);
        }
    }
}
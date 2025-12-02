using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

public record CreateAccountCommand(CreateAccountDTO dto) :  IRequest<BaseResponse<IdentityDTO>>{}

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, BaseResponse<IdentityDTO>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork,  IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper =  mapper;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BaseResponse<IdentityDTO>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userData = User.Create(request.dto.Name, request.dto.Name, request.dto.TelegramId);
            await _accountRepository.AddAsync(userData);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var identity = _mapper.Map<IdentityDTO>(userData);
            return BaseResponse<IdentityDTO>.Success(identity);
        }
        catch (Exception ex)
        {
            return BaseResponse<IdentityDTO>.Fail(ex.Message);
        }
    }
}
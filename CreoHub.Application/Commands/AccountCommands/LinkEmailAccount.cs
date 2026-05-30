using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

/// <summary>
/// Привязывает Email/Google к уже существующему аккаунту (для пользователей, зашедших через Telegram).
/// Вызывается из GoogleResponse когда в OAuth state присутствует link_user_id.
/// </summary>
public record LinkEmailAccountCommand(Guid UserId, string Email)
    : IRequest<BaseResponse<IdentityDTO>>;

public class LinkEmailAccountHandler
    : IRequestHandler<LinkEmailAccountCommand, BaseResponse<IdentityDTO>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork        _unitOfWork;
    private readonly IMapper            _mapper;

    public LinkEmailAccountHandler(
        IAccountRepository accountRepository,
        IUnitOfWork        unitOfWork,
        IMapper            mapper)
    {
        _accountRepository = accountRepository;
        _unitOfWork        = unitOfWork;
        _mapper            = mapper;
    }

    public async Task<BaseResponse<IdentityDTO>> Handle(
        LinkEmailAccountCommand request,
        CancellationToken       cancellationToken)
    {
        try
        {
            // 1. Проверяем, что этот email ещё не привязан к другому аккаунту
            var existingByEmail = await _accountRepository.FindUserByCredentials(request.Email, null);
            if (existingByEmail is not null && existingByEmail.Id != request.UserId)
                return BaseResponse<IdentityDTO>.Fail("Этот Email уже привязан к другому аккаунту.");

            if (existingByEmail is not null && existingByEmail.Id == request.UserId)
                return BaseResponse<IdentityDTO>.Fail("Этот Email уже привязан к вашему аккаунту.");

            // 2. Загружаем текущего пользователя
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user is null)
                return BaseResponse<IdentityDTO>.Fail("Пользователь не найден.");

            // 3. Привязываем
            user.LinkEmail(request.Email);
            _accountRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var identity = _mapper.Map<IdentityDTO>(user);
            return BaseResponse<IdentityDTO>.Success(identity);
        }
        catch (InvalidOperationException ex)
        {
            return BaseResponse<IdentityDTO>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return BaseResponse<IdentityDTO>.Fail(ex.Message);
        }
    }
}

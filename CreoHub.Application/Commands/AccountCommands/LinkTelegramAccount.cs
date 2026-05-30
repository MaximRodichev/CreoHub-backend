using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

/// <summary>
/// Привязывает Telegram к уже существующему аккаунту (для пользователей, зашедших через Google).
/// Требует [Authorize] — userId берётся из JWT.
/// </summary>
public record LinkTelegramAccountCommand(Guid UserId, TelegramAuthData Data)
    : IRequest<BaseResponse<IdentityDTO>>;

public class LinkTelegramAccountHandler
    : IRequestHandler<LinkTelegramAccountCommand, BaseResponse<IdentityDTO>>
{
    private readonly IAccountRepository    _accountRepository;
    private readonly IUnitOfWork           _unitOfWork;
    private readonly IMapper               _mapper;
    private readonly ITelegramAuthVerifier _verifier;

    public LinkTelegramAccountHandler(
        IAccountRepository    accountRepository,
        IUnitOfWork           unitOfWork,
        IMapper               mapper,
        ITelegramAuthVerifier verifier)
    {
        _accountRepository = accountRepository;
        _unitOfWork        = unitOfWork;
        _mapper            = mapper;
        _verifier          = verifier;
    }

    public async Task<BaseResponse<IdentityDTO>> Handle(
        LinkTelegramAccountCommand request,
        CancellationToken          cancellationToken)
    {
        try
        {
            // 1. Верификация подписи Telegram
            if (!_verifier.Verify(request.Data))
                return BaseResponse<IdentityDTO>.Fail("Неверная подпись Telegram. Возможна подделка данных.");

            // 2. Проверяем, что этот Telegram уже не привязан к другому аккаунту
            var existingByTelegram = await _accountRepository.FindUserByCredentials(null, request.Data.Id);
            if (existingByTelegram is not null && existingByTelegram.Id != request.UserId)
                return BaseResponse<IdentityDTO>.Fail("Этот Telegram уже привязан к другому аккаунту.");

            if (existingByTelegram is not null && existingByTelegram.Id == request.UserId)
                return BaseResponse<IdentityDTO>.Fail("Этот Telegram уже привязан к вашему аккаунту.");

            // 3. Загружаем текущего пользователя
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user is null)
                return BaseResponse<IdentityDTO>.Fail("Пользователь не найден.");

            // 4. Привязываем
            user.LinkTelegram(request.Data.Id, request.Data.Username);
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

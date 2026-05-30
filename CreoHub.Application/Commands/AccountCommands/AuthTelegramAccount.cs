using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AccountDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

/// <summary>
/// Авторизация / регистрация через Telegram Login Widget.
/// Если пользователь уже существует — логин, если нет — регистрация.
/// </summary>
public record AuthTelegramAccountCommand(TelegramAuthData Data)
    : IRequest<BaseResponse<IdentityDTO>>;

public class AuthTelegramAccountHandler
    : IRequestHandler<AuthTelegramAccountCommand, BaseResponse<IdentityDTO>>
{
    private readonly IAccountRepository     _accountRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly IMapper                _mapper;
    private readonly ITelegramAuthVerifier  _verifier;

    public AuthTelegramAccountHandler(
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
        AuthTelegramAccountCommand request,
        CancellationToken          cancellationToken)
    {
        try
        {
            // 1. Верификация подписи Telegram
            if (!_verifier.Verify(request.Data))
                return BaseResponse<IdentityDTO>.Fail("Неверная подпись Telegram. Возможна подделка данных.");

            // 2. Ищем пользователя по TelegramId
            var user = await _accountRepository.FindUserByCredentials(null, request.Data.Id);
            if (user is null)
            {
                // Новый пользователь — регистрируем
                var name = BuildName(request.Data);
                user = User.Create(
                    name:             name,
                    telegramId:       request.Data.Id,
                    telegramUsername: request.Data.Username);

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

    private static string BuildName(TelegramAuthData data)
    {
        var full = string.IsNullOrWhiteSpace(data.LastName)
            ? data.FirstName
            : $"{data.FirstName} {data.LastName}";
        return full.Length > 50 ? full[..50] : full;
    }
}

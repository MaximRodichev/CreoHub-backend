using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

/// <summary>Смена отображаемого имени пользователя (профиль). Имена не уникальны.</summary>
public record RenameUserCommand(Guid UserId, string Name) : IRequest<BaseResponse<bool>>;

public class RenameUserHandler : IRequestHandler<RenameUserCommand, BaseResponse<bool>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RenameUserHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(RenameUserCommand request, CancellationToken ct)
    {
        try
        {
            var user = await _accountRepository.GetFullInfoByIdAsync(request.UserId);
            if (user is null)
                return BaseResponse<bool>.Fail("Пользователь не найден.");

            user.Rename(request.Name);   // валидация внутри: непусто, ≤50 символов
            _accountRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return BaseResponse<bool>.Success(true);
        }
        catch (ArgumentException ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
        catch (Exception)
        {
            return BaseResponse<bool>.Fail("Не удалось изменить имя.");
        }
    }
}

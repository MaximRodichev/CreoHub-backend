using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

public record UpdateNotificationSettingsCommand(
    Guid UserId,
    bool NotifyOnPurchase,
    bool NotifyOnModeration
) : IRequest<BaseResponse<bool>>;

public class UpdateNotificationSettingsHandler
    : IRequestHandler<UpdateNotificationSettingsCommand, BaseResponse<bool>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork        _unitOfWork;

    public UpdateNotificationSettingsHandler(
        IAccountRepository accountRepository,
        IUnitOfWork        unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork        = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        UpdateNotificationSettingsCommand request, CancellationToken ct)
    {
        try
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user is null)
                return BaseResponse<bool>.Fail("Пользователь не найден.");

            user.UpdateNotificationSettings(request.NotifyOnPurchase, request.NotifyOnModeration);
            _accountRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

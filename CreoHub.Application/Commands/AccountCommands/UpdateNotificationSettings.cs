using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

public record UpdateNotificationSettingsCommand(
    Guid UserId,
    bool TelegramEnabled,
    bool EmailEnabled,
    bool NotifyOnPurchase,
    bool NotifyOnModeration,
    bool NotifyOnBalance,
    bool NotifyOnBroadcast
) : IRequest<BaseResponse<bool>>;

public class UpdateNotificationSettingsHandler
    : IRequestHandler<UpdateNotificationSettingsCommand, BaseResponse<bool>>
{
    private readonly IAccountRepository      _accountRepository;
    private readonly IUnitOfWork             _unitOfWork;
    private readonly INotificationService    _notifications;

    public UpdateNotificationSettingsHandler(
        IAccountRepository   accountRepository,
        IUnitOfWork          unitOfWork,
        INotificationService notifications)
    {
        _accountRepository = accountRepository;
        _unitOfWork        = unitOfWork;
        _notifications     = notifications;
    }

    public async Task<BaseResponse<bool>> Handle(
        UpdateNotificationSettingsCommand request, CancellationToken ct)
    {
        try
        {
            var user = await _accountRepository.GetByIdWithSettingsAsync(request.UserId, ct);
            if (user is null)
                return BaseResponse<bool>.Fail("Пользователь не найден.");

            if (user.NotificationSettings is null)
                return BaseResponse<bool>.Fail("Настройки уведомлений не найдены.");

            user.NotificationSettings.Update(
                request.TelegramEnabled,
                request.EmailEnabled,
                request.NotifyOnPurchase,
                request.NotifyOnModeration,
                request.NotifyOnBalance,
                request.NotifyOnBroadcast);

            _accountRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            // Подтверждение через каналы (транзиентное, не сохраняем в in-app)
            var tg  = request.TelegramEnabled  ? user.TelegramId    : null;
            var em  = request.EmailEnabled     ? user.EmailAddress  : null;
            var purchase   = request.NotifyOnPurchase   ? "вкл" : "выкл";
            var moderation = request.NotifyOnModeration ? "вкл" : "выкл";
            var balance    = request.NotifyOnBalance    ? "вкл" : "выкл";
            var broadcast  = request.NotifyOnBroadcast  ? "вкл" : "выкл";
            var msg = $"Настройки уведомлений обновлены: " +
                      $"продажи — {purchase}, " +
                      $"модерация — {moderation}, " +
                      $"баланс — {balance}, " +
                      $"рассылки — {broadcast}.";
            _ = _notifications.SendAsync(tg, em, msg, CancellationToken.None);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ShopRequests;

// ── Reply ─────────────────────────────────────────────────────────────────────

public record ReplyToShopRequestCommand(Guid ShopId, int RequestId, string Text)
    : IRequest<BaseResponse<bool>>;

public class ReplyToShopRequestHandler : IRequestHandler<ReplyToShopRequestCommand, BaseResponse<bool>>
{
    private const int MaxReplyLength = 2000;

    private readonly IShopRequestRepository _requests;
    private readonly IShopRepository        _shops;
    private readonly IAccountRepository     _accounts;
    private readonly INotificationService   _notifications;
    private readonly IUnitOfWork            _unitOfWork;

    public ReplyToShopRequestHandler(
        IShopRequestRepository requests, IShopRepository shops, IAccountRepository accounts,
        INotificationService notifications, IUnitOfWork unitOfWork)
    {
        _requests      = requests;
        _shops         = shops;
        _accounts      = accounts;
        _notifications = notifications;
        _unitOfWork    = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(ReplyToShopRequestCommand request, CancellationToken ct)
    {
        try
        {
            var text = request.Text?.Trim() ?? string.Empty;
            if (text.Length == 0)
                return BaseResponse<bool>.Fail("Ответ не может быть пустым.");
            if (text.Length > MaxReplyLength)
                return BaseResponse<bool>.Fail($"Ответ слишком длинный (макс. {MaxReplyLength} символов).");

            var req = await _requests.GetByIdAsync(request.RequestId, ct);
            if (req is null)
                return BaseResponse<bool>.Fail("Предложение не найдено.");
            if (req.ShopId != request.ShopId)
                return BaseResponse<bool>.Fail("Нет доступа к этому предложению.");

            req.Reply(text);
            await _unitOfWork.SaveChangesAsync(ct);

            _ = ShopRequestNotifier.NotifyBuyerAsync(
                _accounts, _shops, _notifications, req.BuyerUserId, req.ShopId,
                replied: true, payload: text);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

// ── Decline ───────────────────────────────────────────────────────────────────

public record DeclineShopRequestCommand(Guid ShopId, int RequestId, string? Reason)
    : IRequest<BaseResponse<bool>>;

public class DeclineShopRequestHandler : IRequestHandler<DeclineShopRequestCommand, BaseResponse<bool>>
{
    private const int MaxReasonLength = 2000;

    private readonly IShopRequestRepository _requests;
    private readonly IShopRepository        _shops;
    private readonly IAccountRepository     _accounts;
    private readonly INotificationService   _notifications;
    private readonly IUnitOfWork            _unitOfWork;

    public DeclineShopRequestHandler(
        IShopRequestRepository requests, IShopRepository shops, IAccountRepository accounts,
        INotificationService notifications, IUnitOfWork unitOfWork)
    {
        _requests      = requests;
        _shops         = shops;
        _accounts      = accounts;
        _notifications = notifications;
        _unitOfWork    = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(DeclineShopRequestCommand request, CancellationToken ct)
    {
        try
        {
            var reason = request.Reason?.Trim();
            if (reason is { Length: > MaxReasonLength })
                return BaseResponse<bool>.Fail($"Причина слишком длинная (макс. {MaxReasonLength} символов).");

            var req = await _requests.GetByIdAsync(request.RequestId, ct);
            if (req is null)
                return BaseResponse<bool>.Fail("Предложение не найдено.");
            if (req.ShopId != request.ShopId)
                return BaseResponse<bool>.Fail("Нет доступа к этому предложению.");

            req.Decline(reason);
            await _unitOfWork.SaveChangesAsync(ct);

            _ = ShopRequestNotifier.NotifyBuyerAsync(
                _accounts, _shops, _notifications, req.BuyerUserId, req.ShopId,
                replied: false, payload: reason);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

// ── Общий помощник уведомления покупателя ──────────────────────────────────────

internal static class ShopRequestNotifier
{
    public static async Task NotifyBuyerAsync(
        IAccountRepository accounts, IShopRepository shops, INotificationService notifications,
        Guid buyerId, Guid shopId, bool replied, string? payload)
    {
        try
        {
            var buyer = await accounts.GetByIdWithSettingsAsync(buyerId);
            var ns = buyer?.NotificationSettings;
            if (buyer is null || ns is null) return;

            var shop = await shops.GetShopByIdAsync(shopId);
            var shopName = shop?.Name ?? "магазин";

            string msg;
            if (replied)
            {
                var preview = (payload ?? string.Empty);
                preview = preview.Length > 200 ? preview[..200] + "…" : preview;
                msg = $"Магазин «{shopName}» ответил на ваше предложение: {preview}";
            }
            else
            {
                msg = string.IsNullOrWhiteSpace(payload)
                    ? $"Магазин «{shopName}» отклонил ваше предложение."
                    : $"Магазин «{shopName}» отклонил ваше предложение. Причина: {payload}";
            }

            var tg = ns.TelegramEnabled ? buyer.TelegramId   : null;
            var em = ns.EmailEnabled    ? buyer.EmailAddress : null;

            await notifications.NotifyAsync(buyer.Id, NotificationType.ShopRequest,
                msg, actionUrl: "/me/requests", tg, em, CancellationToken.None);
        }
        catch { /* уведомления не должны влиять на основной поток */ }
    }
}

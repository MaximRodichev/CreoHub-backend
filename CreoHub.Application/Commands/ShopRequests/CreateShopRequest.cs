using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ShopRequests;

public record CreateShopRequestCommand(Guid BuyerUserId, Guid ShopId, string Message)
    : IRequest<BaseResponse<bool>>;

public class CreateShopRequestHandler : IRequestHandler<CreateShopRequestCommand, BaseResponse<bool>>
{
    private const int MaxMessageLength = 1000;
    private const int MaxPerDay        = 5;

    private readonly IShopRequestRepository _requests;
    private readonly IShopRepository        _shops;
    private readonly IAccountRepository     _accounts;
    private readonly INotificationService   _notifications;
    private readonly IUnitOfWork            _unitOfWork;

    public CreateShopRequestHandler(
        IShopRequestRepository requests,
        IShopRepository        shops,
        IAccountRepository     accounts,
        INotificationService   notifications,
        IUnitOfWork            unitOfWork)
    {
        _requests      = requests;
        _shops         = shops;
        _accounts      = accounts;
        _notifications = notifications;
        _unitOfWork    = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(CreateShopRequestCommand request, CancellationToken ct)
    {
        try
        {
            var message = request.Message?.Trim() ?? string.Empty;
            if (message.Length == 0)
                return BaseResponse<bool>.Fail("Сообщение не может быть пустым.");
            if (message.Length > MaxMessageLength)
                return BaseResponse<bool>.Fail($"Сообщение слишком длинное (макс. {MaxMessageLength} символов).");

            var shop = await _shops.GetShopByIdAsync(request.ShopId);
            if (shop is null)
                return BaseResponse<bool>.Fail("Магазин не найден.");
            if (shop.OwnerId == request.BuyerUserId)
                return BaseResponse<bool>.Fail("Нельзя оставить предложение собственному магазину.");

            // ── Анти-спам ───────────────────────────────────────────────────────
            if (await _requests.HasOpenRequestAsync(request.BuyerUserId, request.ShopId, ct))
                return BaseResponse<bool>.Fail(
                    "У вас уже есть активное предложение этому магазину. Дождитесь ответа продавца.");

            var since = DateTime.UtcNow.AddDays(-1);
            if (await _requests.CountByBuyerSinceAsync(request.BuyerUserId, since, ct) >= MaxPerDay)
                return BaseResponse<bool>.Fail(
                    "Слишком много предложений за последние сутки. Попробуйте позже.");

            // ── Снапшот покупателя ───────────────────────────────────────────────
            var buyer = await _accounts.GetByIdAsync(request.BuyerUserId);
            if (buyer is null)
                return BaseResponse<bool>.Fail("Пользователь не найден.");

            var entity = ShopRequest.Create(
                request.ShopId, request.BuyerUserId, buyer.Name, buyer.EmailAddress, message);

            await _requests.AddAsync(entity, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // Fire-and-forget уведомление продавцу
            _ = NotifySellerAsync(request.ShopId, shop.Name, entity.BuyerName, message);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }

    private async Task NotifySellerAsync(Guid shopId, string shopName, string buyerName, string message)
    {
        try
        {
            var seller = await _accounts.GetUserByShopIdAsync(shopId);
            var ns = seller?.NotificationSettings;
            if (seller is null || ns is null) return;

            var preview = message.Length > 200 ? message[..200] + "…" : message;
            var msg = $"Новое предложение в магазин «{shopName}» от {buyerName}: {preview}";
            var tg  = ns.TelegramEnabled ? seller.TelegramId   : null;
            var em  = ns.EmailEnabled    ? seller.EmailAddress : null;

            await _notifications.NotifyAsync(seller.Id, NotificationType.ShopRequest,
                msg, actionUrl: "/shop/requests", tg, em, CancellationToken.None);
        }
        catch { /* уведомления не должны влиять на основной поток */ }
    }
}

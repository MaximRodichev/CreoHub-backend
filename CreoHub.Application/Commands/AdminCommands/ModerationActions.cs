using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.Commands.ShopFollows;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Application.Commands.AdminCommands;

public record ApproveModerationCommand(int ProductId, Guid AdminUserId)
    : IRequest<BaseResponse<bool>>;

public record RejectModerationCommand(int ProductId, Guid AdminUserId, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

// ── Approve ─────────────────────────────────────────────────────────────────

public class ApproveModerationHandler : IRequestHandler<ApproveModerationCommand, BaseResponse<bool>>
{
    private readonly IProductRepository          _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IAccountRepository          _accountRepository;
    private readonly INotificationService        _notifications;
    private readonly IServiceScopeFactory        _scopeFactory;
    private readonly IUnitOfWork                 _unitOfWork;

    public ApproveModerationHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IAccountRepository accountRepository,
        INotificationService notifications,
        IServiceScopeFactory scopeFactory,
        IUnitOfWork unitOfWork)
    {
        _productRepository    = productRepository;
        _statusLogRepository  = statusLogRepository;
        _accountRepository    = accountRepository;
        _notifications        = notifications;
        _scopeFactory         = scopeFactory;
        _unitOfWork           = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(ApproveModerationCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Product not found.");

            var oldStatus = product.ProductStatus;

            // ДО добавления новой записи лога: публиковался ли товар раньше и когда был прошлый аппрув.
            var priorActivations = (await _statusLogRepository.GetByProductIdAsync(product.Id, ct)
                                    ?? new List<ProductStatusLog>())
                .Where(l => l.NewStatus == ProductStatus.Active)
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
            var everPublished = priorActivations.Count > 0;

            product.ApproveModeration();
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.Active,
                reason: "Модерация пройдена — одобрено администратором",
                changedById: request.AdminUserId), ct);

            await _unitOfWork.SaveChangesAsync(ct);

            // Продавцу — синхронно (1 получатель, текущий DbContext ещё жив)
            await NotifySellerAsync(product.OwnerId, product.Name, approved: true, ct);

            // Решение об уведомлении: первая публикация → «новый товар» подписчикам;
            // повторная — «подорожал/подешевел» только cart-юзерам при изменении цены; иначе молчим.
            ApprovalNotification decision;
            if (!everPublished)
            {
                decision = ApprovalNotification.NewProduct;
            }
            else
            {
                var prevApproveAt      = priorActivations[0].CreatedAt;
                var lastPublishedPrice = product.Prices
                    .Where(p => p.Date <= prevApproveAt)
                    .OrderByDescending(p => p.Date)
                    .Select(p => (decimal?)p.Value)
                    .FirstOrDefault();
                decision = ApprovalNotificationPolicy.Decide(true, lastPublishedPrice, product.GetCurrentPrice());
            }

            switch (decision)
            {
                case ApprovalNotification.NewProduct:
                    // в фоне, в собственном scope (свой DbContext, не блокирует ответ)
                    _ = ShopFollowerNotifier.NotifyNewProductInScopeAsync(
                        _scopeFactory, product.OwnerId, product.Name, product.Slug);
                    break;
                case ApprovalNotification.PriceUp:
                case ApprovalNotification.PriceDown:
                    _ = ProductPriceChangeNotifier.NotifyInScopeAsync(
                        _scopeFactory, product.Id, product.Name, product.Slug,
                        wentUp: decision == ApprovalNotification.PriceUp);
                    break;
                case ApprovalNotification.None:
                default:
                    break;
            }

            // og:image карточка товара → R2 (в фоне, свой scope)
            _ = ProductOgGenerator.GenerateInScopeAsync(_scopeFactory, product.Id);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }

    private async Task NotifySellerAsync(Guid shopId, string productName, bool approved, CancellationToken ct)
    {
        try
        {
            var seller = await _accountRepository.GetUserByShopIdAsync(shopId, ct);
            var ns = seller?.NotificationSettings;
            if (seller is null || ns is null || !ns.NotifyOnModeration) return;

            var msg = $"Продукт «{productName}» прошёл модерацию и теперь доступен покупателям.";
            var tg  = ns.TelegramEnabled ? seller.TelegramId   : null;
            var em  = ns.EmailEnabled    ? seller.EmailAddress : null;
            await _notifications.NotifyAsync(seller.Id, Domain.Types.NotificationType.Moderation,
                msg, actionUrl: null, tg, em, CancellationToken.None);
        }
        catch { /* notifications must never affect the main flow */ }
    }
}

// ── Reject ──────────────────────────────────────────────────────────────────

public class RejectModerationHandler : IRequestHandler<RejectModerationCommand, BaseResponse<bool>>
{
    private readonly IProductRepository          _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IAccountRepository          _accountRepository;
    private readonly INotificationService        _notifications;
    private readonly IUnitOfWork                 _unitOfWork;

    public RejectModerationHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IAccountRepository accountRepository,
        INotificationService notifications,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _accountRepository   = accountRepository;
        _notifications       = notifications;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(RejectModerationCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Product not found.");

            var oldStatus = product.ProductStatus;
            product.RejectModeration();
            _productRepository.Update(product);

            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Отклонено администратором"
                : request.Reason;

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.ModerationFailed,
                reason: reason,
                changedById: request.AdminUserId), ct);

            await _unitOfWork.SaveChangesAsync(ct);

            // Fire-and-forget notification to seller
            _ = NotifySellerAsync(product.OwnerId, product.Name, reason, ct);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }

    private async Task NotifySellerAsync(Guid shopId, string productName, string reason, CancellationToken ct)
    {
        try
        {
            var seller = await _accountRepository.GetUserByShopIdAsync(shopId, ct);
            var ns = seller?.NotificationSettings;
            if (seller is null || ns is null || !ns.NotifyOnModeration) return;

            var msg = $"Продукт «{productName}» не прошёл модерацию. Причина: {reason}";
            var tg  = ns.TelegramEnabled ? seller.TelegramId   : null;
            var em  = ns.EmailEnabled    ? seller.EmailAddress : null;
            await _notifications.NotifyAsync(seller.Id, Domain.Types.NotificationType.Moderation,
                msg, actionUrl: null, tg, em, CancellationToken.None);
        }
        catch { /* notifications must never affect the main flow */ }
    }
}

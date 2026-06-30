using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Pricing;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Services;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static CreoHub.Domain.Services.BundleCalculator;

namespace CreoHub.Application.Commands.OrderCommands;

/// <summary>
/// Checkout Path B — мгновенная оплата с баланса пользователя без OxaPay.
/// </summary>
public record CheckoutWithBalanceCommand(
    Guid                  UserId,
    List<CheckoutItemDTO> Items,
    string?               SessionId = null
) : IRequest<BaseResponse<CheckoutResultDTO>>;

public class CheckoutWithBalanceHandler
    : IRequestHandler<CheckoutWithBalanceCommand, BaseResponse<CheckoutResultDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IUserBalanceRepository _balanceRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IContentAccessRepository _accessRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IShopTransactionRepository _shopTransactionRepository;
    private readonly IShopBalanceRepository _shopBalanceRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IEventTracker _events;
    private readonly INotificationService _notifications;

    private readonly PricingConfig _pricing;

    public CheckoutWithBalanceHandler(
        IUnitOfWork unitOfWork,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserTransactionRepository transactionRepository,
        IUserBalanceRepository balanceRepository,
        IContentFileRepository contentFileRepository,
        IContentAccessRepository accessRepository,
        ICartRepository cartRepository,
        IShopTransactionRepository shopTransactionRepository,
        IShopBalanceRepository shopBalanceRepository,
        IAccountRepository accountRepository,
        IOptions<PricingConfig> pricing,
        IEventTracker events,
        INotificationService notifications)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _transactionRepository = transactionRepository;
        _balanceRepository = balanceRepository;
        _contentFileRepository = contentFileRepository;
        _accessRepository = accessRepository;
        _cartRepository = cartRepository;
        _shopTransactionRepository = shopTransactionRepository;
        _shopBalanceRepository = shopBalanceRepository;
        _accountRepository = accountRepository;
        _pricing = pricing.Value;
        _events  = events;
        _notifications = notifications;
    }

    public async Task<BaseResponse<CheckoutResultDTO>> Handle(
        CheckoutWithBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                return BaseResponse<CheckoutResultDTO>.Fail("Items list cannot be empty.");

            // Ранняя проверка существования баланса (без блокировки)
            var balanceExists = await _balanceRepository.GetByUserIdAsync(request.UserId);
            if (balanceExists is null)
                return BaseResponse<CheckoutResultDTO>.Fail("Insufficient balance.");

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products   = await _productRepository.GetProductsByIds(productIds);

            if (products.Count != productIds.Count)
                return BaseResponse<CheckoutResultDTO>.Fail("Some products were not found.");

            // ── Проверяем что все товары доступны для покупки ──────────
            var unavailable = products
                .Where(p => p.ProductStatus != ProductStatus.Active)
                .ToList();
            if (unavailable.Any())
            {
                var names = string.Join(", ", unavailable.Select(p => $"«{p.Name}»"));
                return BaseResponse<CheckoutResultDTO>.Fail(
                    $"Следующие товары недоступны для покупки: {names}. " +
                    "Возможно, они были сняты с продажи или заблокированы.");
            }

            var productMap = products.ToDictionary(p => p.Id);

            // ── Загружаем дочерние продукты для бандлов ──────────────
            var bundleChildIds = products
                .Where(p => p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(b => b.ProductId))
                .Distinct()
                .Except(productMap.Keys)
                .ToList();

            if (bundleChildIds.Count > 0)
            {
                var childProducts = await _productRepository.GetProductsByIds(bundleChildIds);
                foreach (var cp in childProducts)
                    productMap.TryAdd(cp.Id, cp);
            }

            // ── Проверка коллизии: товар И бандл с этим товаром одновременно ──
            // Например: Chicken Coin + ChickenDrisnya (содержит Chicken Coin).
            // Это вызвало бы дублирующий ContentAccess и UNIQUE constraint violation.
            var allBundleChildIdSet = products
                .Where(p => p.ProductType == ProductType.Bundle)
                .SelectMany(p => p.BundleItems.Select(b => b.ProductId))
                .ToHashSet();

            var conflictIds = productIds
                .Where(id => productMap.TryGetValue(id, out var p)
                             && p.ProductType != ProductType.Bundle
                             && allBundleChildIdSet.Contains(id))
                .ToList();

            if (conflictIds.Any())
            {
                var names = string.Join(", ", conflictIds.Select(id => $"«{productMap[id].Name}»"));
                return BaseResponse<CheckoutResultDTO>.Fail(
                    $"Нельзя купить одновременно товар и набор, в который он входит: {names}. " +
                    "Удалите отдельный товар из корзины — он уже включён в набор.");
            }

            // ── Загружаем уже купленные файлы пользователя ───────────
            // Делается ДО любых изменений баланса или БД.
            var ownedAccesses = await _accessRepository.GetByUserIdAsync(request.UserId);
            var ownedFileIds  = ownedAccesses.Select(a => a.ContentFileId).ToHashSet();

            // Словарь скорректированных цен бандлов с частичным владением
            var bundleAdjustedPrices = new Dictionary<int, decimal>();

            // ── Формируем позиции заказа + проверка владения ─────────
            var orderItems = new List<(Product product, List<ContentFile> selectedFiles)>();
            foreach (var item in request.Items)
            {
                var product = productMap[item.ProductId];
                List<ContentFile> selectedFiles;

                if (item.FileIds.Count == 0)
                {
                    if (product.ProductType == ProductType.Bundle)
                    {
                        // ── Бандл: загружаем файлы всех дочерних продуктов одним запросом ──
                        var childProductIds = product.BundleItems.Select(b => b.ProductId).ToList();
                        var allChildFiles   = await _contentFileRepository.GetByProductIdsAsync(childProductIds);
                        var filesByChild    = allChildFiles.GroupBy(f => f.ProductId)
                                                           .ToDictionary(g => g.Key, g => g.ToList());

                        // Если у бандла нет дочерних файлов — продаём по полной цене без проверок
                        if (allChildFiles.Count > 0)
                        {
                            // Все ли файлы уже куплены?
                            if (allChildFiles.All(f => ownedFileIds.Contains(f.Id)))
                                return BaseResponse<CheckoutResultDTO>.Fail(
                                    $"Вы уже приобрели все файлы из набора «{product.Name}».");

                            // Строим параметры для BundleCalculator: (basePrice, totalWeight, ownedWeight)
                            // Цену каждого дочернего продукта берём из productMap (если уже загружен),
                            // либо запрашиваем при необходимости.
                            var childParams = new List<(decimal BasePrice, int TotalWeight, int OwnedWeight, int TotalFiles)>();
                            foreach (var bundleItem in product.BundleItems)
                            {
                                if (!productMap.TryGetValue(bundleItem.ProductId, out var childProduct))
                                    continue; // продукт не в запросе — пропускаем

                                var childFileList  = filesByChild.GetValueOrDefault(bundleItem.ProductId, new List<ContentFile>());
                                var totalWeight    = childFileList.Sum(f => f.PriceWeight);
                                var ownedWeight    = childFileList.Where(f => ownedFileIds.Contains(f.Id))
                                                                  .Sum(f => f.PriceWeight);
                                childParams.Add((childProduct.GetCurrentPrice(), totalWeight, ownedWeight, childFileList.Count));
                            }

                            if (childParams.Count > 0)
                            {
                                var adj = BundleCalculator.Calculate(product.GetCurrentPrice(), childParams, _pricing.ComputeAlpha);

                                if (adj.ExceedsThreshold)
                                    return BaseResponse<CheckoutResultDTO>.Fail(
                                        $"Вы уже купили более 50% стоимости набора «{product.Name}». " +
                                        "Приобретайте оставшиеся товары по отдельности.");

                                // Переопределяем цену бандла в заказе через расчётную
                                bundleAdjustedPrices[item.ProductId] = adj.FinalPrice;
                            }
                        }

                        selectedFiles = new List<ContentFile>();
                    }
                    else
                    {
                        // Полная покупка — автоматически исключаем уже купленные файлы
                        var allFiles = product.ContentFiles.ToList();
                        var nonOwned = allFiles.Where(cf => !ownedFileIds.Contains(cf.Id)).ToList();

                        // Только если у продукта есть файлы и все уже куплены — блокируем
                        if (allFiles.Count > 0 && nonOwned.Count == 0)
                            return BaseResponse<CheckoutResultDTO>.Fail(
                                $"Все файлы продукта «{product.Name}» уже куплены вами.");

                        // Если часть файлов уже куплена — покупаем только оставшиеся
                        // Если файлов нет или ни одного не куплено — полная покупка (пустой список)
                        selectedFiles = (allFiles.Count > 0 && nonOwned.Count < allFiles.Count)
                            ? nonOwned
                            : new List<ContentFile>();
                    }
                }
                else
                {
                    // Частичная покупка — отклоняем, если хоть один запрошенный файл уже куплен
                    var alreadyOwned = item.FileIds.Where(id => ownedFileIds.Contains(id)).ToList();
                    if (alreadyOwned.Any())
                        return BaseResponse<CheckoutResultDTO>.Fail(
                            $"Некоторые файлы из «{product.Name}» уже куплены вами.");

                    selectedFiles = product.ContentFiles
                        .Where(cf => item.FileIds.Contains(cf.Id))
                        .ToList();

                    if (selectedFiles.Count != item.FileIds.Count)
                        return BaseResponse<CheckoutResultDTO>.Fail(
                            $"Some content files for product {item.ProductId} were not found.");
                }

                orderItems.Add((product, selectedFiles));
            }

            var order = Order.Open(
                description:    string.Empty,
                items:          orderItems,
                customerId:     request.UserId,
                priceOverrides: bundleAdjustedPrices.Count > 0 ? bundleAdjustedPrices : null,
                computeAlpha:   _pricing.ComputeAlpha);

            // ── Рассчитываем и сохраняем снимок скидок ──────────────
            // Велком (−20% на первый заказ) капается потолком в $ (User.FirstOrderDiscountCap),
            // лояльность и объём — нет. Итог = лучшая по сумме (см. Order.ApplyDiscounts).
            var user         = await _accountRepository.GetFullInfoByIdAsync(request.UserId);
            var welcomeDisc  = user?.GetWelcomeDiscount()  ?? 0m;
            var lifetimeDisc = user?.GetLifetimeDiscount() ?? 0m;
            var cartDisc     = DiscountCalculator.GetCartCountDiscount(orderItems.Count);
            order.ApplyDiscounts(welcomeDisc, lifetimeDisc, cartDisc, User.FirstOrderDiscountCap);

            var buyerPays = order.Price; // уже пересчитан после ApplyDiscounts

            // ── Пессимистичная блокировка + списание баланса ─────────
            // BEGIN TRANSACTION → SELECT ... FOR UPDATE блокирует строку.
            // Конкурентный запрос будет ждать пока мы не закоммитим/откатим.
            await _unitOfWork.BeginTransactionAsync();
            bool txCommitted = false;
            try
            {
                var balance = await _balanceRepository.GetByUserIdForUpdateAsync(request.UserId);
                if (balance is null || balance.AvailableAmount < buyerPays)
                    return BaseResponse<CheckoutResultDTO>.Fail(
                        $"Insufficient balance. Required: {buyerPays:F2}, available: {balance?.AvailableAmount ?? 0:F2}.");

                balance.Spend(buyerPays);
                _balanceRepository.Update(balance);

            // Order.Id — это GUID, генерируется на стороне клиента в конструкторе Order.
            // Поэтому промежуточный SaveChanges не нужен: собираем всё в контексте,
            // коммитим один раз в конце — атомарно.
            await _orderRepository.AddAsync(order);

            // ── Транзакция + завершение заказа ──────────────────────
            var trackId     = $"balance-{Guid.NewGuid()}";
            var transaction = UserTransaction.CreatePurchase(buyerPays, request.UserId, trackId, order);
            transaction.SuccessInternal();

            await _transactionRepository.AddAsync(transaction);
            // AttachTransaction НЕ вызываем: EF fix-up сам линкует order.Transaction
            // при AddAsync(transaction) где transaction.Order = order.
            // Явный вызов выбрасывал бы "Order already has a transaction".
            order.Complete();

            // ── Зачисляем выручку на баланс магазина (ShopBalance) ───
            // Автор всегда получает от rawTotal (скидка поглощается платформой).
            // Группируем по shopId на случай будущего маркетплейса.
            var shopRevenue = order.Items
                .GroupBy(item => productMap[item.ProductId].OwnerId)
                .Select(g => new { ShopId = g.Key, Amount = g.Sum(i => i.PriceAtPurchase) })
                .ToList();  // PriceAtPurchase записан из rawTotal через Order.Open()

            foreach (var revenue in shopRevenue)
            {
                var shopTx = ShopTransaction.CreateShopSale(
                    amount:  revenue.Amount,
                    shopId:  revenue.ShopId,
                    trackId: $"sale-{order.Id}-{revenue.ShopId}",
                    order:   order);
                shopTx.SuccessInternal();
                await _shopTransactionRepository.AddAsync(shopTx);

                var shopBalance = await _shopBalanceRepository.GetByShopIdAsync(revenue.ShopId);
                if (shopBalance is null)
                {
                    shopBalance = new ShopBalance(revenue.ShopId);
                    shopBalance.AddFunds(shopTx.NetAmount);
                    await _shopBalanceRepository.AddAsync(shopBalance);
                }
                else
                {
                    shopBalance.AddFunds(shopTx.NetAmount);
                    _shopBalanceRepository.Update(shopBalance);
                }
            }

            // ── Выдаём ContentAccess ─────────────────────────────────
            // grantedFileIds: дедупликация внутри одного чекаута.
            // Защищает от UNIQUE constraint если один и тот же файл попадает
            // сразу через обычный товар и через бандл (например, при валидационном пропуске).
            var grantedFileIds = new HashSet<Guid>(ownedFileIds); // уже купленные тоже исключаем

            foreach (var item in order.Items)
            {
                if (item.Files.Any())
                {
                    // Частичная покупка — только выбранные файлы
                    foreach (var file in item.Files)
                    {
                        if (grantedFileIds.Add(file.ContentFileId)) // Add возвращает false если уже есть
                            await _accessRepository.AddAsync(
                                new ContentAccess(order.CustomerId, file.ContentFileId, order.Id));
                    }
                }
                else
                {
                    var product = productMap[item.ProductId];

                    if (product.ProductType == ProductType.Bundle)
                    {
                        // Бандл — выдаём доступ ко всем файлам каждого дочернего продукта
                        foreach (var bundleItem in product.BundleItems)
                        {
                            var childFiles = await _contentFileRepository.GetByProductIdAsync(bundleItem.ProductId);
                            foreach (var file in childFiles)
                            {
                                if (grantedFileIds.Add(file.Id))
                                    await _accessRepository.AddAsync(
                                        new ContentAccess(order.CustomerId, file.Id, order.Id));
                            }
                        }
                    }
                    else
                    {
                        // Обычный продукт — полная покупка всех файлов
                        var allFiles = await _contentFileRepository.GetByProductIdAsync(item.ProductId);
                        foreach (var file in allFiles)
                        {
                            if (grantedFileIds.Add(file.Id))
                                await _accessRepository.AddAsync(
                                    new ContentAccess(order.CustomerId, file.Id, order.Id));
                        }
                    }
                }
            }

                // Единый атомарный коммит: баланс + ордер + транзакции + доступы
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();
                txCommitted = true;
            }
            finally
            {
                // Если выход через исключение или ранний return — откатываем
                if (!txCommitted)
                    await _unitOfWork.RollbackTransactionAsync();
            }

            // ── Обновляем LifetimeSpent пользователя (F1) ───────────
            if (user != null)
            {
                user.AddSpend(order.Price);    // реально уплаченная сумма (после скидок), = buyerPays
                _accountRepository.Update(user);
                try { await _unitOfWork.SaveChangesAsync(cancellationToken); } catch { /* не критично */ }
            }

            // ── Удаляем купленные товары из корзины ─────────────────
            // Делается после commit — если не удалится из корзины, это не критично
            foreach (var productId in productIds)
            {
                try
                {
                    var cartItem = await _cartRepository.GetCartItemByUserAndProduct(request.UserId, productId);
                    if (cartItem != null)
                    {
                        await _cartRepository.RemoveCartItem(cartItem);
                    }
                }
                catch
                {
                    // Товара может не быть в корзине — не критично
                }
            }

            try { await _unitOfWork.SaveChangesAsync(cancellationToken); } catch { /* игнорируем */ }

            // ── Analytics events ──────────────────────────────────────────
            _events.Track(EventTypes.CheckoutCompleted,
                userId:    request.UserId,
                sessionId: request.SessionId);

            foreach (var productId in productIds)
                _events.Track(EventTypes.ProductPurchased,
                    productId: productId,
                    userId:    request.UserId,
                    sessionId: request.SessionId);

            // ── Уведомляем продавцов о покупке (fire-and-forget) ─────────────
            // Загружаем данные продавцов ДО fire-and-forget, пока DbContext жив.
            // Фоновая задача получает готовые сообщения — без обращений к БД.
            var notifTargets = await BuildSellerNotificationsAsync(productMap, order, cancellationToken);
            _ = SendSellerNotificationsAsync(notifTargets);

            return BaseResponse<CheckoutResultDTO>.Success(new CheckoutResultDTO
            {
                OrderId    = order.Id,
                PaymentUrl = string.Empty,
                ExpiresAt  = DateTime.UtcNow
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return BaseResponse<CheckoutResultDTO>.Fail("Баланс изменился во время оплаты. Попробуйте ещё раз.");
        }
        catch (Exception)
        {
            return BaseResponse<CheckoutResultDTO>.Fail("Не удалось завершить оплату. Попробуйте позже.");
        }
    }

    /// <summary>
    /// Синхронно (в рамках живого DbContext) собирает получателей и тексты уведомлений.
    /// </summary>
    private async Task<List<(Guid UserId, long? TelegramId, string? Email, string Message)>> BuildSellerNotificationsAsync(
        Dictionary<int, Domain.Entities.Product> productMap,
        Order order,
        CancellationToken ct)
    {
        var result = new List<(Guid, long?, string?, string)>();
        try
        {
            var shopIds = productMap.Values.Select(p => p.OwnerId).Distinct();
            foreach (var shopId in shopIds)
            {
                var seller = await _accountRepository.GetUserByShopIdAsync(shopId, ct);
                var ns = seller?.NotificationSettings;
                if (seller is null || ns is null || !ns.NotifyOnPurchase) continue;

                var productNames = productMap.Values
                    .Where(p => p.OwnerId == shopId)
                    .Select(p => p.Name);

                var amount = order.Items
                    .Where(i => productMap.ContainsKey(i.ProductId) && productMap[i.ProductId].OwnerId == shopId)
                    .Sum(i => i.PriceAtPurchase);

                result.Add((seller.Id,
                    ns.TelegramEnabled ? seller.TelegramId   : null,
                    ns.EmailEnabled    ? seller.EmailAddress : null,
                    $"Новая продажа: {string.Join(", ", productNames)}. Сумма: {amount:F2} USDT"));
            }
        }
        catch { /* не критично */ }
        return result;
    }

    /// <summary>
    /// Fire-and-forget: только HTTP-вызовы + in-app, без обращений к DbContext.
    /// </summary>
    private async Task SendSellerNotificationsAsync(
        List<(Guid UserId, long? TelegramId, string? Email, string Message)> targets)
    {
        try
        {
            foreach (var (userId, tg, email, msg) in targets)
                await _notifications.NotifyAsync(userId, Domain.Types.NotificationType.Purchase,
                    msg, actionUrl: null, tg, email, CancellationToken.None);
        }
        catch { }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.OrderCommands;

/// <summary>
/// Checkout Path B — мгновенная оплата с баланса пользователя без OxaPay.
/// </summary>
public record CheckoutWithBalanceCommand(Guid UserId, List<CheckoutItemDTO> Items)
    : IRequest<BaseResponse<CheckoutResultDTO>>;

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
        IShopBalanceRepository shopBalanceRepository)
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
    }

    public async Task<BaseResponse<CheckoutResultDTO>> Handle(
        CheckoutWithBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Items == null || request.Items.Count == 0)
                return BaseResponse<CheckoutResultDTO>.Fail("Items list cannot be empty.");

            var balance = await _balanceRepository.GetByUserIdAsync(request.UserId);
            if (balance is null)
                return BaseResponse<CheckoutResultDTO>.Fail("Insufficient balance.");

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products   = await _productRepository.GetProductsByIds(productIds);

            if (products.Count != productIds.Count)
                return BaseResponse<CheckoutResultDTO>.Fail("Some products were not found.");

            var productMap = products.ToDictionary(p => p.Id);

            // ── Загружаем уже купленные файлы пользователя ───────────
            // Делается ДО любых изменений баланса или БД.
            var ownedAccesses = await _accessRepository.GetByUserIdAsync(request.UserId);
            var ownedFileIds  = ownedAccesses.Select(a => a.ContentFileId).ToHashSet();

            // ── Формируем позиции заказа + проверка владения ─────────
            var orderItems = new List<(Product product, List<ContentFile> selectedFiles)>();
            foreach (var item in request.Items)
            {
                var product = productMap[item.ProductId];
                List<ContentFile> selectedFiles;

                if (item.FileIds.Count == 0)
                {
                    // Полная покупка — автоматически исключаем уже купленные файлы
                    var allFiles     = product.ContentFiles.ToList();
                    var nonOwned     = allFiles.Where(cf => !ownedFileIds.Contains(cf.Id)).ToList();

                    if (nonOwned.Count == 0)
                        return BaseResponse<CheckoutResultDTO>.Fail(
                            $"Все файлы продукта «{product.Name}» уже куплены вами.");

                    // Если часть файлов уже куплена — покупаем только оставшиеся (частичная покупка)
                    // Если ни одного нет — настоящая полная покупка (пустой список = GetCurrentPrice)
                    selectedFiles = nonOwned.Count < allFiles.Count
                        ? nonOwned
                        : new List<ContentFile>();
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

            var order = Order.Open(description: string.Empty, items: orderItems, customerId: request.UserId);

            // ── Проверяем и списываем баланс ─────────────────────────
            if (balance.AvailableAmount < order.Price)
                return BaseResponse<CheckoutResultDTO>.Fail(
                    $"Insufficient balance. Required: {order.Price:F2}, available: {balance.AvailableAmount:F2}.");

            balance.Spend(order.Price);
            _balanceRepository.Update(balance);

            // Сохраняем Order первым — чтобы его Id попал в БД до создания транзакции
            await _orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── Транзакция + завершение заказа ──────────────────────
            var trackId     = $"balance-{Guid.NewGuid()}";
            var transaction = UserTransaction.CreatePurchase(order.Price, request.UserId, trackId, order);
            transaction.SuccessInternal();

            await _transactionRepository.AddAsync(transaction);
            order.AttachTransaction(transaction);
            order.Complete();

            // ── Зачисляем выручку на баланс магазина (ShopBalance) ───
            // Используем PriceAtPurchase из OrderItem — там уже корректная цена.
            // Группируем по shopId на случай будущего маркетплейса.
            var shopRevenue = order.Items
                .GroupBy(item => productMap[item.ProductId].OwnerId)
                .Select(g => new { ShopId = g.Key, Amount = g.Sum(i => i.PriceAtPurchase) })
                .ToList();

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
            foreach (var item in order.Items)
            {
                if (item.Files.Any())
                {
                    foreach (var file in item.Files)
                        await _accessRepository.AddAsync(
                            new ContentAccess(order.CustomerId, file.ContentFileId, order.Id));
                }
                else
                {
                    var allFiles = await _contentFileRepository.GetByProductIdAsync(item.ProductId);
                    foreach (var file in allFiles)
                        await _accessRepository.AddAsync(
                            new ContentAccess(order.CustomerId, file.Id, order.Id));
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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

            return BaseResponse<CheckoutResultDTO>.Success(new CheckoutResultDTO
            {
                OrderId    = order.Id,
                PaymentUrl = string.Empty,
                ExpiresAt  = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<CheckoutResultDTO>.Fail(ex.Message);
        }
    }
}

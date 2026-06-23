using System.ComponentModel.DataAnnotations.Schema;
using CreoHub.Domain.Services;
using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = new();
    private decimal _price;

    public Guid Id { get; private init; } = Guid.NewGuid();

    /// <summary>Сумма к оплате покупателем (после скидок). До вызова ApplyDiscounts равна Subtotal.</summary>
    public decimal Price => _price;

    /// <summary>Сумма без скидок — просто сумма позиций заказа.</summary>
    public decimal Subtotal { get; private set; }

    /// <summary>Персональная скидка (lifetime spent), доля [0,1].</summary>
    public decimal PersonalDiscountPercent { get; private set; }

    /// <summary>Скидка за объём корзины, доля [0,1].</summary>
    public decimal CartDiscountPercent { get; private set; }

    /// <summary>Итоговая скидка (сумма личной и объёмной), доля [0,1].</summary>
    public decimal DiscountPercent { get; private set; }

    /// <summary>Абсолютная экономия в валюте заказа.</summary>
    public decimal DiscountAmount { get; private set; }

    public string Description { get; private init; } = string.Empty;

    public DateTime OrderDate { get; private init; } = DateTime.UtcNow;
    public OrderStatus Status { get; private set; } = OrderStatus.Created;

    //FK
    public User Customer { get; private init; }
    public Guid CustomerId { get; private init; }
    [NotMapped]
    public UserTransaction? Transaction { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() {}


    
    /// <summary>
    /// Создание заказа.
    /// </summary>
    /// <param name="priceOverrides">
    ///   Переопределения цены по productId — используется для бандлов
    ///   с частичным владением, где расчётная цена отличается от stored price.
    /// </param>
    public static Order Open(string description,
        List<(Product product, List<ContentFile> selectedFiles)> items,
        Guid customerId,
        Dictionary<int, decimal>? priceOverrides = null,
        Func<int, double>? computeAlpha = null)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Items cannot be empty.", nameof(items));

        var order = new Order
        {
            Description = description,
            CustomerId  = customerId,
        };

        foreach (var (product, selectedFiles) in items)
        {
            IEnumerable<Guid>? purchasedFileIds;
            decimal price;

            if (priceOverrides != null && priceOverrides.TryGetValue(product.Id, out var overridePrice))
            {
                // Скорректированная цена (например, для частичного бандла)
                price            = overridePrice;
                purchasedFileIds = null;
            }
            else if (selectedFiles == null || selectedFiles.Count == 0)
            {
                // Полная покупка — берём текущую цену, файлы не фиксируем
                price            = product.GetCurrentPrice();
                purchasedFileIds = null;
            }
            else
            {
                // Частичная покупка — цена по весу выбранных файлов, α из PricingConfig
                // Все вызывающие стороны передают computeAlpha из PricingConfig; 1.0 = линейный fallback
                var n     = product.ContentFiles.Count;
                var alpha = computeAlpha != null ? computeAlpha(n) : 1.0;
                price            = product.CalculatePrice(selectedFiles, alpha);
                purchasedFileIds = selectedFiles.Select(f => f.Id);
            }

            order._items.Add(new OrderItem(
                orderId:          order.Id,
                productId:        product.Id,
                priceAtPurchase:  price,
                selectedFileIds:  purchasedFileIds
            ));
        }

        order._price  = order._items.Sum(i => i.PriceAtPurchase);
        order.Subtotal = order._price;

        return order;
    }

    /// <summary>
    /// Сохраняет снимок скидок и пересчитывает итоговую сумму к оплате (Price).
    /// Вызывается один раз — сразу после Order.Open(), до сохранения в БД.
    /// Велком капается абсолютным потолком (welcomeCap), лояльность и объём — нет.
    /// Итог считается по суммам (DiscountCalculator.GetBestDiscountAmount), поэтому
    /// крупный опт может обойти упёршийся в потолок велком.
    /// </summary>
    /// <param name="welcomeDiscountPercent">Доля велком-скидки [0,1] (0, если не первый заказ).</param>
    /// <param name="lifetimeDiscountPercent">Доля скидки за лояльность [0,1].</param>
    /// <param name="cartDiscountPercent">Доля скидки за объём корзины [0,1].</param>
    /// <param name="welcomeCap">Потолок экономии велком-скидки в валюте заказа.</param>
    public void ApplyDiscounts(
        decimal welcomeDiscountPercent,
        decimal lifetimeDiscountPercent,
        decimal cartDiscountPercent,
        decimal welcomeCap)
    {
        // Снимок: персональная = лучшая из велком/лояльности (как предлагалась), объёмная — отдельно.
        PersonalDiscountPercent = Math.Max(welcomeDiscountPercent, lifetimeDiscountPercent);
        CartDiscountPercent     = cartDiscountPercent;
        DiscountAmount          = Math.Round(
            DiscountCalculator.GetBestDiscountAmount(
                Subtotal, welcomeDiscountPercent, lifetimeDiscountPercent, cartDiscountPercent, welcomeCap),
            2);
        // Эффективная доля скидки (после капа) — для снимка/истории.
        DiscountPercent = Subtotal > 0m ? Math.Round(DiscountAmount / Subtotal, 4) : 0m;
        _price          = Subtotal - DiscountAmount;
    }

    public void AttachTransaction(UserTransaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
        if (Transaction != null)
            throw new InvalidOperationException("Order already has a transaction.");

        Transaction = transaction;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be completed.");
        // Transaction == null допускается: EF Core fix-up автоматически заполняет
        // навигационное свойство при AddAsync(transaction) с Order = order,
        // поэтому явный AttachTransaction не нужен и его вызов вызывал бы дублирование.

        Status = OrderStatus.Completed;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException("Only created orders can be cancelled.");
        Status = OrderStatus.Cancelled;
    }
}
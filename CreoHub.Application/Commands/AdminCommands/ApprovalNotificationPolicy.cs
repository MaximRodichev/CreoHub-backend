namespace CreoHub.Application.Commands.AdminCommands;

/// <summary>Какое уведомление слать при одобрении товара модератором.</summary>
public enum ApprovalNotification { None, NewProduct, PriceUp, PriceDown }

/// <summary>
/// Чистое правило выбора уведомления при аппруве (тестируется отдельно от хендлера):
///   • первая публикация (никогда не был Active) → NewProduct (подписчикам магазина);
///   • повторная публикация — только при изменении цены относительно прошлой публикации
///     (PriceUp / PriceDown, cart-юзерам, in-app);
///   • прочее (описание, теги, файлы и т.п.) → None.
/// </summary>
public static class ApprovalNotificationPolicy
{
    public static ApprovalNotification Decide(bool everPublished, decimal? lastPublishedPrice, decimal currentPrice)
    {
        if (!everPublished)               return ApprovalNotification.NewProduct;
        if (!lastPublishedPrice.HasValue) return ApprovalNotification.None;
        if (currentPrice > lastPublishedPrice.Value) return ApprovalNotification.PriceUp;
        if (currentPrice < lastPublishedPrice.Value) return ApprovalNotification.PriceDown;
        return ApprovalNotification.None;
    }
}

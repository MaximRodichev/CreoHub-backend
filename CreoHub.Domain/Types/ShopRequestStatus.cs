namespace CreoHub.Domain.Types;

/// <summary>
/// Статус предложения покупателя магазину.
///   New      — отправлено, продавец ещё не ответил (считается в бейдже).
///   Replied  — продавец написал ответ.
///   Declined — продавец отклонил (с причиной или без).
/// </summary>
public enum ShopRequestStatus
{
    New,
    Replied,
    Declined,
}

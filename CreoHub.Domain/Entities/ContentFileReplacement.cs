using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

/// <summary>
/// Заявка продавца на замену байтов купленного контент-файла — только через модерацию.
/// Новый файл (NewStorageObjectId) лежит как staging и НЕ перетирает старый до аппрува.
/// На аппруве модератор подменяет байты на исходном StorageObject (ContentFile/ContentAccess
/// не трогаются → покупатели получают исправленный файл).
/// </summary>
public class ContentFileReplacement
{
    public Guid Id { get; private init; } = Guid.NewGuid();

    public Guid ContentFileId      { get; private init; }   // какой контент-файл заменяем
    public Guid ShopId             { get; private init; }   // владелец (проверка прав)
    public Guid NewStorageObjectId { get; private init; }   // загруженный новый файл (staging)

    public ReplacementStatus Status { get; private set; } = ReplacementStatus.Pending;
    public string? RejectReason     { get; private set; }

    public DateTime  CreatedAt    { get; private init; } = DateTime.UtcNow;
    public DateTime? ReviewedAt   { get; private set; }
    public Guid?     ReviewedById { get; private set; }

    private ContentFileReplacement() {}

    public static ContentFileReplacement Create(Guid contentFileId, Guid shopId, Guid newStorageObjectId)
        => new()
        {
            ContentFileId      = contentFileId,
            ShopId             = shopId,
            NewStorageObjectId = newStorageObjectId,
        };

    public void Approve(Guid adminId)
    {
        if (Status != ReplacementStatus.Pending)
            throw new InvalidOperationException("Only pending replacements can be approved.");
        Status       = ReplacementStatus.Approved;
        ReviewedById = adminId;
        ReviewedAt   = DateTime.UtcNow;
    }

    public void Reject(Guid adminId, string? reason)
    {
        if (Status != ReplacementStatus.Pending)
            throw new InvalidOperationException("Only pending replacements can be rejected.");
        Status       = ReplacementStatus.Rejected;
        ReviewedById = adminId;
        ReviewedAt   = DateTime.UtcNow;
        RejectReason = reason;
    }
}

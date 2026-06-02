namespace CreoHub.Domain.Entities;

/// <summary>
/// Запись о запрошенной presigned-загрузке.
/// Создаётся в request-upload, удаляется в confirm-upload.
/// Гарантирует что key принадлежит магазину и не подменён клиентом.
/// </summary>
public class PendingUpload
{
    public Guid   Id       { get; private init; } = Guid.NewGuid();
    public string Key      { get; private init; } = string.Empty;
    public Guid   ShopId   { get; private init; }
    public string FileName { get; private init; } = string.Empty;
    public string MimeType { get; private init; } = string.Empty;
    /// <summary>Лимит размера, вычисленный на request-upload (FileLimits.For(mime)). 0 = без лимита.</summary>
    public long   MaxBytes { get; private init; }
    public DateTime ExpiresAt { get; private init; }

    private PendingUpload() {}

    public static PendingUpload Create(string key, Guid shopId, string fileName, string mimeType,
        long maxBytes, TimeSpan? ttl = null) =>
        new()
        {
            Key       = key,
            ShopId    = shopId,
            FileName  = fileName ?? string.Empty,
            MimeType  = mimeType,
            MaxBytes  = maxBytes,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromHours(2)),
        };

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

namespace CreoHub.API.Configuration;

/// <summary>
/// Единственная точка входа для всех лимитов размера файлов в проекте.
/// Используется в S3Controller, ShopController и Program.cs.
/// </summary>
public static class FileLimits
{
    // ── Per-MIME-category limits (для загрузки через /s3/upload и /s3/request-upload) ──

    private static readonly Dictionary<string, long> _byMimeCategory = new()
    {
        { "video",       50L  * 1024 * 1024        }, // 50 MB
        { "image",       5L   * 1024 * 1024        }, // 5 MB
        { "application", 4L   * 1024 * 1024 * 1024 }, // 4 GB (content files)
    };

    // ── Глобальные константы ──────────────────────────────────────────────────────

    /// <summary>Максимальный размер тела запроса для Kestrel и legacy /s3/upload (4 GB).</summary>
    public const long MaxUpload = 4L * 1024 * 1024 * 1024;

    /// <summary>Лимит для загрузки декораций магазина (баннер / логотип).</summary>
    public const long Decoration = 10L * 1024 * 1024; // 10 MB

    /// <summary>Дефолтный лимит для неизвестных MIME-категорий.</summary>
    public const long Default = 10L * 1024 * 1024; // 10 MB

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>Возвращает лимит размера файла по его MIME-типу (например "video/mp4").</summary>
    public static long For(string mimeType)
    {
        var category = mimeType.Split('/')[0];
        return _byMimeCategory.TryGetValue(category, out var limit) ? limit : Default;
    }

    /// <summary>Форматирует лимит в человекочитаемый вид (MB или GB).</summary>
    public static string Format(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / 1024 / 1024 / 1024} GB";
        return $"{bytes / 1024 / 1024} MB";
    }
}

namespace CreoHub.Application.Services;

public interface IStorageService
{
    public Task<string> UploadFileAsync(Stream fileStream, string key, string contentType);
    public Task<bool> DeleteFileAsync(string key);
    Task DownloadFileAsync(string key, string destinationPath);
    string GeneratePresignedUrl(string key, int expiresInMinutes = 60);
    string GeneratePresignedUrl(string key, int expiresInMinutes, string contentDisposition);
    Task<string> GeneratePresignedUploadUrlAsync(string key, string mimeType, int expiresInMinutes = 30);
    Task<bool> FileExistsAsync(string key);

    /// <summary>
    /// Открывает стрим для чтения файла из R2 (без загрузки в память).
    /// Caller обязан dispose стрим после использования.
    /// </summary>
    Task<Stream> OpenReadStreamAsync(string key);

    /// <summary>
    /// Листинг всех объектов в основном бакете с пагинацией.
    /// Используется для поиска orphan-файлов (есть в R2, нет в БД).
    /// </summary>
    IAsyncEnumerable<(string Key, DateTime LastModified)> ListAllObjectsAsync();
}
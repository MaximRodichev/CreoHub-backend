namespace CreoHub.Application.Services;

public interface IStorageService
{
    public Task<string> UploadFileAsync(Stream fileStream, string key, string contentType);
    public Task<bool> DeleteFileAsync(string key);
    Task DownloadFileAsync(string key, string destinationPath);
    string GeneratePresignedUrl(string key, int expiresInMinutes = 60);
    Task<string> GeneratePresignedUploadUrlAsync(string key, string mimeType, int expiresInMinutes = 30);
    Task<bool> FileExistsAsync(string key);
}
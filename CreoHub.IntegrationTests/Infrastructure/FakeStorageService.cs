using CreoHub.Application.Services;

namespace CreoHub.IntegrationTests.Infrastructure;

public sealed class FakeStorageService : IStorageService
{
    public Task<string> UploadFileAsync(Stream fileStream, string key, string contentType)
        => Task.FromResult(key);

    public Task<bool> DeleteFileAsync(string key)
        => Task.FromResult(true);

    public async Task DownloadFileAsync(string key, string destinationPath)
    {
        await File.WriteAllTextAsync(destinationPath, $"fake storage object: {key}");
    }

    public string GeneratePresignedUrl(string key, int expiresInMinutes = 60)
        => $"https://storage.integration.test/{Uri.EscapeDataString(key)}?expires={expiresInMinutes}";

    public string GeneratePresignedUrl(string key, int expiresInMinutes, string contentDisposition)
        => $"https://storage.integration.test/{Uri.EscapeDataString(key)}?expires={expiresInMinutes}&disposition={Uri.EscapeDataString(contentDisposition)}";

    public Task<string> GeneratePresignedUploadUrlAsync(string key, string mimeType, int expiresInMinutes = 30)
        => Task.FromResult($"https://upload.integration.test/{Uri.EscapeDataString(key)}?mime={Uri.EscapeDataString(mimeType)}&expires={expiresInMinutes}");

    public Task<bool> FileExistsAsync(string key)
        => Task.FromResult(true);
}

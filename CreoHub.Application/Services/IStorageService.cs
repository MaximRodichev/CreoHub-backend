namespace CreoHub.Application.Services;

public interface IStorageService
{
    public Task<string> UploadFileAsync(string fromFilePath, string key, string contentType);
    public Task<bool> DeleteFileAsync(string key);
}
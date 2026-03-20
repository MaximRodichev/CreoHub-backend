namespace CreoHub.Application.Services;

public interface IStorageService
{
    public Task<string> UploadFileAsync(string filePath, string key, string contentType);
}
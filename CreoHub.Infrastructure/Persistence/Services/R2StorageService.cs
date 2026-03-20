using Amazon.S3;
using Amazon.S3.Model;
using CreoHub.Application.Services;

namespace CreoHub.Infrastructure.Persistence.Services;

public class R2StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private const string BucketName = "creohub";

    public R2StorageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }
    
    public async Task<string> UploadFileAsync(string filePath, string key, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            FilePath = filePath,
            ContentType = contentType,
            DisablePayloadSigning = true // Обязательно для Cloudflare R2
        };

        var response = await _s3Client.PutObjectAsync(request);
        
        return $"https://pub-27e74704e7594d30b9ff3e6cc1000e9b.r2.dev/{key}";
    }
}
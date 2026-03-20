using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CreoHub.Application.Services;

namespace CreoHub.Infrastructure.Persistence.Services;

public class R2StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private const string BucketMainName = "creohub";
    private const string BucketPreviewName = "creohub-preview";
    private const string BucketContentType = "creohub-content";

    public R2StorageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }
    
    public async Task<string> UploadFileAsync(string filePath, string key, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketMainName,
            Key = key,
            FilePath = filePath,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        var response = await _s3Client.PutObjectAsync(request);
        
        return response.HttpStatusCode.ToString();
    }

    public async Task<bool> DeleteFileAsync(string key)
    {
        var request = new DeleteObjectRequest()
        {
            BucketName = BucketMainName,
            Key = key
        };

        var response = await _s3Client.DeleteObjectAsync(request);
        
        return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
    }
}
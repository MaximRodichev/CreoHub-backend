using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using CreoHub.Application.Services;

namespace CreoHub.Infrastructure.Persistence.Services;

public class R2StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    //TODO: test bucket now usages
    private const string BucketMainName = "creohub";
    private const string BucketPreviewName = "creohub-preview";
    private const string BucketContentType = "creohub-content";

    public R2StorageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }
    
    public async Task<string> UploadFileAsync(Stream fileStream, string key, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketMainName,
            Key = key,
            InputStream = fileStream,
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

    public async Task DownloadFileAsync(string key, string destinationPath)
    {
        var request = new GetObjectRequest
        {
            BucketName = BucketMainName,
            Key = key
        };
        var response = await _s3Client.GetObjectAsync(request);
        await response.WriteResponseStreamToFileAsync(destinationPath, false, CancellationToken.None);
    }
    
    public string GeneratePresignedUrl(string key, int expiresInMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketMainName,
            Key        = key,
            Expires    = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Protocol   = Protocol.HTTPS,
            Verb       = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public string GeneratePresignedUrl(string key, int expiresInMinutes, string contentDisposition)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName               = BucketMainName,
            Key                      = key,
            Expires                  = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Protocol                 = Protocol.HTTPS,
            Verb                     = HttpVerb.GET,
            ResponseHeaderOverrides  = new ResponseHeaderOverrides
            {
                ContentDisposition = contentDisposition
            }
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public Task<string> GeneratePresignedUploadUrlAsync(string key, string mimeType, int expiresInMinutes = 30)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName  = BucketMainName,
            Key         = key,
            Expires     = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Protocol    = Protocol.HTTPS,
            Verb        = HttpVerb.PUT,
            ContentType = mimeType
        };

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task<Stream> OpenReadStreamAsync(string key)
    {
        var request  = new GetObjectRequest { BucketName = BucketMainName, Key = key };
        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    public async Task<bool> FileExistsAsync(string key)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(BucketMainName, key);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async IAsyncEnumerable<(string Key, DateTime LastModified)> ListAllObjectsAsync()
    {
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName        = BucketMainName,
                MaxKeys           = 1000,
                ContinuationToken = continuationToken,
            };

            var response = await _s3Client.ListObjectsV2Async(request);

            foreach (var obj in response.S3Objects)
                yield return (obj.Key, obj.LastModified ?? DateTime.MinValue);

            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken != null);
    }
}
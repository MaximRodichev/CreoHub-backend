using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace CreoHub.Infrastructure.Persistence.Services;

public class ThumbnailGenerationService : IThumbnailGenerationService
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMediaProductRepository  _mediaProductRepository;
    private readonly IStorageService          _storageService;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<ThumbnailGenerationService> _logger;

    public ThumbnailGenerationService(
        IStorageObjectRepository storageObjectRepository,
        IMediaProductRepository  mediaProductRepository,
        IStorageService          storageService,
        IUnitOfWork              unitOfWork,
        ILogger<ThumbnailGenerationService> logger)
    {
        _storageObjectRepository = storageObjectRepository;
        _mediaProductRepository  = mediaProductRepository;
        _storageService          = storageService;
        _unitOfWork              = unitOfWork;
        _logger                  = logger;
    }

    public async Task GenerateAsync(Guid storageObjectId, CancellationToken cancellationToken)
    {
        var video = await _storageObjectRepository.GetByIdAsync(storageObjectId);
        if (video is null) return;

        // Работаем только с видео-медиа (не с content-файлами)
        if (video.FileType == FileType.Content) return;
        if (!IsVideo(video.MimeType))           return;

        var mediaProduct = await _mediaProductRepository.GetByStorageObjectIdAsync(storageObjectId);
        if (mediaProduct is null) return;

        // Thumbnail уже есть — пропускаем
        if (mediaProduct.ThumbnailId.HasValue) return;

        var ext          = Path.GetExtension(video.Key);
        var inputPath    = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");
        var thumbPath    = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");

        try
        {
            _logger.LogInformation("Generating thumbnail for StorageObject {Id}", storageObjectId);

            await _storageService.DownloadFileAsync(video.Key, inputPath);

            // Берём кадр на 3-й секунде; если видео короче — FFmpeg возьмёт первый доступный
            await FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputPath}\"")
                .AddParameter("-ss 00:00:03")
                .AddParameter("-vframes 1")
                .AddParameter("-q:v 2")
                .SetOutput(thumbPath)
                .Start(cancellationToken);

            // Ключ thumbnail рядом с оригиналом: videos/abc.webm → videos/abc_thumb.jpg
            var thumbKey = Path.ChangeExtension(video.Key, null) + "_thumb.jpg";

            await using var stream = File.OpenRead(thumbPath);
            await _storageService.UploadFileAsync(stream, thumbKey, "image/jpeg");

            // StorageObject для thumbnail — берём OwnerId от исходного видео
            var thumbObject = new StorageObject(
                thumbKey,
                $"{Path.GetFileNameWithoutExtension(video.FileName)}_thumb.jpg",
                new FileInfo(thumbPath).Length,
                "image/jpeg",
                video.OwnerId);
            thumbObject.ChangeFileType(FileType.Thumbnail);

            await _storageObjectRepository.AddAsync(thumbObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken); // нужен Id для FK

            mediaProduct.SetThumbnail(thumbObject.Id);
            _mediaProductRepository.Update(mediaProduct);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Thumbnail saved as {Key}", thumbKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail generation failed for StorageObject {Id}", storageObjectId);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(thumbPath)) File.Delete(thumbPath);
        }
    }

    private static bool IsVideo(string? mimeType) =>
        mimeType is not null && mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
}

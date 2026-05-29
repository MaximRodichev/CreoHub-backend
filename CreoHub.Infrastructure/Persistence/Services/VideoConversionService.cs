using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace CreoHub.Infrastructure.Persistence.Services;

public class VideoConversionService : IVideoConversionService
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<VideoConversionService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public VideoConversionService(IUnitOfWork unitOfWork, IStorageObjectRepository storageObjectRepository, IStorageService storageService, ILogger<VideoConversionService> logger)
    {
        _storageObjectRepository = storageObjectRepository;
        _storageService = storageService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task ConvertAsync(Guid storageObjectId, CancellationToken cancellationToken)
    {
        var video = await _storageObjectRepository.GetByIdAsync(storageObjectId);

        if (video == null) return;
        if (video.FileType == FileType.Content)
        {
            return;
        }
        
        var inputPath  = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");

        try
        {
            _logger.LogInformation("Starting H.264 conversion for {Key}", video.Key);

            await _storageService.DownloadFileAsync(video.Key, inputPath);

            // H.264/AAC — в 3–4× быстрее VP9, памяти в разы меньше, поддержка везде
            // CRF 30 = агрессивное сжатие, scale=-2:720 = 720p с сохранением пропорций
            // faststart = MP4-атом moov в начале файла (стриминг без полной загрузки)
            await FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputPath}\"")
                .AddParameter("-c:v libx264 -crf 30 -preset slow")
                .AddParameter("-vf scale=-2:720")
                .AddParameter("-c:a aac -b:a 64k")
                .AddParameter("-movflags +faststart")
                .SetOutput(outputPath)
                .Start(cancellationToken);

            var newKey = Path.ChangeExtension(video.Key, ".mp4");
            using var stream = File.OpenRead(outputPath);
            await _storageService.UploadFileAsync(stream, newKey, "video/mp4");
            // Удаляем старый файл только если ключ изменился (e.g. .webm → .mp4).
            // Если ключ тот же (уже .mp4), UploadFileAsync уже перезаписал файл — удалять не нужно.
            if (!string.Equals(newKey, video.Key, StringComparison.OrdinalIgnoreCase))
                await _storageService.DeleteFileAsync(video.Key);

            video.ReplaceFile(newKey, video.FileName, new FileInfo(outputPath).Length, "video/mp4");

            _storageObjectRepository.Update(video);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("H.264 conversion done {Key}", newKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Conversion failed for {Key}: {Error}", video.Key, ex.Message);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
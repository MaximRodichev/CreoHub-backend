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
        
        var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.webm");

        try
        {
            _logger.LogInformation("Starting conversion for {Key}", video.Key);

            await _storageService.DownloadFileAsync(video.Key, inputPath);

            await FFmpeg.Conversions.New()
                .AddParameter($"-i {inputPath}")
                .AddParameter("-c:v libvpx-vp9 -crf 40 -b:v 0")
                .AddParameter("-c:a libopus -b:a 64k")
                .AddParameter("-deadline realtime")
                .SetOutput(outputPath)
                .Start(cancellationToken);

            var newKey = Path.ChangeExtension(video.Key, ".webm");
            using var stream = File.OpenRead(outputPath);
            await _storageService.UploadFileAsync(stream, newKey, "video/webm");
            await _storageService.DeleteFileAsync(video.Key);
            
            video.ReplaceFile(newKey, video.FileName, new FileInfo(outputPath).Length, "video/webm");
            
            _storageObjectRepository.Update(video);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Conversion done {Key}", newKey);
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
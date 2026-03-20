using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using FFMpegCore;
using FFMpegCore.Enums;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

// Команда, которую вызывает контроллер
public record UploadProductMediaCommand(Stream FileStream, string FileName, int ProductId) : IRequest;

public class UploadProductMediaHandler : IRequestHandler<UploadProductMediaCommand>
{
    private readonly IStorageService _storage;
    private readonly IStorageObjectRepository _storageObject;
    private readonly IMediaProductRepository _mediaProductRepository;

    public UploadProductMediaHandler(IStorageService storage)
    {
        _storage = storage;
    }

    public async Task Handle(UploadProductMediaCommand request, CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(request.FileName));
        using (var fs = new FileStream(tempPath, FileMode.Create))
        {
            await request.FileStream.CopyToAsync(fs);
        }

        _ = Task.Run(async () => await ProcessVideoAsync(tempPath, request.ProductId));
    }

    private async Task ProcessVideoAsync(string inputPath, int productId)
    {
        var webmPath = Path.ChangeExtension(inputPath, ".webm");
        var thumbPath = Path.ChangeExtension(inputPath, ".webp");
        
        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(webmPath, true, options => options
                    .WithVideoCodec("libvpx-vp9")
                    .WithConstantRateFactor(28) // Чем выше, тем меньше вес (оптимально 24-30
                    .WithCustomArgument("-b:v 1000k") 
                    .WithCustomArgument("-row-mt 1"))
                .ProcessAsynchronously();

            // Б. Создание Thumbnail (10-й кадр)
            await FFMpeg.SnapshotAsync(inputPath, thumbPath, null, TimeSpan.FromSeconds(0.5));
            // В. Загрузка в Cloudflare R2
            var videoUrl = await _storage.UploadFileAsync(webmPath, $"products/{productId}/video.webm", "video/webm");
            var thumbUrl = await _storage.UploadFileAsync(thumbPath, $"products/{productId}/thumb.webp", "image/webp");
            Console.WriteLine($"Uploaded video: {videoUrl}, thumb: {thumbUrl}");
            // // Г. Запись в БД
            var response = "";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally 
        {
            // Чистим временные файлы
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(webmPath)) File.Delete(webmPath);
            if (File.Exists(thumbPath)) File.Delete(thumbPath);
        }
    }
}
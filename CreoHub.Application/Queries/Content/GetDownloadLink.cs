using System.Text.RegularExpressions;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Content;

public record GetDownloadLinkQuery(Guid UserId, Guid ContentFileId)
    : IRequest<BaseResponse<string>>;

public class GetDownloadLinkHandler : IRequestHandler<GetDownloadLinkQuery, BaseResponse<string>>
{
    private readonly IContentAccessRepository _accessRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IStorageService _storageService;

    public GetDownloadLinkHandler(
        IContentAccessRepository accessRepository,
        IContentFileRepository contentFileRepository,
        IStorageService storageService)
    {
        _accessRepository = accessRepository;
        _contentFileRepository = contentFileRepository;
        _storageService = storageService;
    }

    public async Task<BaseResponse<string>> Handle(
        GetDownloadLinkQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var hasAccess = await _accessRepository.HasAccessAsync(request.UserId, request.ContentFileId);
            if (!hasAccess)
                throw new UnauthorizedAccessException("Access denied to this content file.");

            var contentFile = await _contentFileRepository.GetByIdWithStorageAsync(request.ContentFileId)
                ?? throw new InvalidOperationException("Content file not found.");

            // Строим имя файла: "{ProductName} — {PreviewName}.{ext}"
            var ext             = Path.GetExtension(contentFile.StorageObject.FileName); // ".zip", ".png" ...
            var productName     = contentFile.Product?.Name ?? "File";
            var displayName     = contentFile.PreviewName;
            var rawFileName     = $"{productName} — {displayName}{ext}";
            var safeFileName    = SanitizeFileName(rawFileName);
            var contentDisp     = $"attachment; filename=\"{safeFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(safeFileName)}";

            var url = _storageService.GeneratePresignedUrl(
                contentFile.StorageObject.Key,
                expiresInMinutes: 10,
                contentDisposition: contentDisp);

            return BaseResponse<string>.Success(url);
        }
        catch (UnauthorizedAccessException ex)
        {
            return BaseResponse<string>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return BaseResponse<string>.Fail(ex.Message);
        }
    }

    /// <summary>Убирает символы, недопустимые в именах файлов.</summary>
    private static string SanitizeFileName(string name)
    {
        // Заменяем недопустимые символы на подчёркивание
        var invalid = new string(Path.GetInvalidFileNameChars());
        var pattern = $"[{Regex.Escape(invalid)}]";
        var sanitized = Regex.Replace(name, pattern, "_");
        // Убираем двойные пробелы/подчёркивания
        sanitized = Regex.Replace(sanitized, @"_{2,}", "_");
        return sanitized.Trim('_', ' ');
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Storage;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

/// <summary>Контент-файл товара для проверки админом: метаданные + presigned-URL на скачивание.</summary>
public record AdminContentFileDTO(
    Guid   Id,
    string PreviewName,
    string FileName,
    long   FileSize,
    string MimeType,
    bool   IsArchived,
    string DownloadUrl);

/// <summary>
/// Все контент-файлы товара с presigned-ссылками на скачивание (БЕЗ проверки владельца — только админ).
/// Используется при модерации, чтобы проверить, что внутри файлов. TTL ссылок короткий.
/// </summary>
public record GetAdminProductContentFilesQuery(int ProductId)
    : IRequest<BaseResponse<List<AdminContentFileDTO>>>;

public class GetAdminProductContentFilesHandler
    : IRequestHandler<GetAdminProductContentFilesQuery, BaseResponse<List<AdminContentFileDTO>>>
{
    private const int DownloadTtlMinutes = 5;

    private readonly IContentFileRepository   _contentFiles;
    private readonly IStorageObjectRepository _storageObjects;
    private readonly IStorageService          _storage;

    public GetAdminProductContentFilesHandler(
        IContentFileRepository contentFiles,
        IStorageObjectRepository storageObjects,
        IStorageService storage)
    {
        _contentFiles   = contentFiles;
        _storageObjects = storageObjects;
        _storage        = storage;
    }

    public async Task<BaseResponse<List<AdminContentFileDTO>>> Handle(
        GetAdminProductContentFilesQuery request, CancellationToken ct)
    {
        var files = await _contentFiles.GetByProductIdAsync(request.ProductId);
        if (files.Count == 0)
            return BaseResponse<List<AdminContentFileDTO>>.Success(new List<AdminContentFileDTO>());

        var storageIds = files.Select(f => f.StorageObjectId).Distinct().ToList();
        var storage    = await _storageObjects.GetByIdsAsync(storageIds);
        var byId       = storage.ToDictionary(s => s.Id);

        var result = files.Select(f =>
        {
            byId.TryGetValue(f.StorageObjectId, out var so);
            var url = so is null
                ? string.Empty
                : _storage.GeneratePresignedUrl(so.Key, DownloadTtlMinutes,
                    OwnerDownloadHandler.BuildContentDisposition(so.FileName));
            return new AdminContentFileDTO(
                f.Id, f.PreviewName,
                so?.FileName ?? string.Empty,
                so?.FileSize ?? 0,
                so?.MimeType ?? string.Empty,
                f.IsArchived, url);
        }).ToList();

        return BaseResponse<List<AdminContentFileDTO>>.Success(result);
    }
}

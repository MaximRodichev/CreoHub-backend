using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Storage;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

/// <summary>Заявка на замену для проверки админом: старый и новый файлы + presigned-ссылки для сравнения.</summary>
public record PendingReplacementDTO(
    Guid     Id,
    int      ProductId,
    string   ProductName,
    Guid     ContentFileId,
    string   OldFileName,
    long     OldFileSize,
    string   OldDownloadUrl,
    string   NewFileName,
    long     NewFileSize,
    string   NewDownloadUrl,
    DateTime CreatedAt);

public record GetPendingContentReplacementsQuery()
    : IRequest<BaseResponse<List<PendingReplacementDTO>>>;

public class GetPendingContentReplacementsHandler
    : IRequestHandler<GetPendingContentReplacementsQuery, BaseResponse<List<PendingReplacementDTO>>>
{
    private const int DownloadTtlMinutes = 10;

    private readonly IContentFileReplacementRepository _replacements;
    private readonly IContentFileRepository            _contentFiles;
    private readonly IStorageObjectRepository          _storageObjects;
    private readonly IStorageService                   _storage;

    public GetPendingContentReplacementsHandler(
        IContentFileReplacementRepository replacements,
        IContentFileRepository contentFiles,
        IStorageObjectRepository storageObjects,
        IStorageService storage)
    {
        _replacements   = replacements;
        _contentFiles   = contentFiles;
        _storageObjects = storageObjects;
        _storage        = storage;
    }

    public async Task<BaseResponse<List<PendingReplacementDTO>>> Handle(
        GetPendingContentReplacementsQuery request, CancellationToken ct)
    {
        var pending = await _replacements.GetPendingAsync(ct);
        var result  = new List<PendingReplacementDTO>(pending.Count);

        foreach (var r in pending)
        {
            var cf      = await _contentFiles.GetByIdWithStorageAsync(r.ContentFileId);
            var oldSo   = cf?.StorageObject;
            var product = cf?.Product;
            var newSo   = await _storageObjects.GetByIdAsync(r.NewStorageObjectId);

            var oldUrl = oldSo is null ? string.Empty
                : _storage.GeneratePresignedUrl(oldSo.Key, DownloadTtlMinutes,
                    OwnerDownloadHandler.BuildContentDisposition(oldSo.FileName));
            var newUrl = newSo is null ? string.Empty
                : _storage.GeneratePresignedUrl(newSo.Key, DownloadTtlMinutes,
                    OwnerDownloadHandler.BuildContentDisposition(newSo.FileName));

            result.Add(new PendingReplacementDTO(
                r.Id,
                product?.Id   ?? 0,
                product?.Name ?? string.Empty,
                r.ContentFileId,
                oldSo?.FileName ?? string.Empty, oldSo?.FileSize ?? 0, oldUrl,
                newSo?.FileName ?? string.Empty, newSo?.FileSize ?? 0, newUrl,
                r.CreatedAt));
        }

        return BaseResponse<List<PendingReplacementDTO>>.Success(result);
    }
}

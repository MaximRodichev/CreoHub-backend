using System.Text;
using System.Text.RegularExpressions;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using MediatR;

namespace CreoHub.Application.Queries.Storage;

// ── Single file ────────────────────────────────────────────────────────────────

public record OwnerDownloadQuery(Guid FileId, Guid ShopId)
    : IRequest<BaseResponse<OwnerDownloadResult>>;

public record OwnerDownloadResult(string DownloadUrl, string FileName);

public class OwnerDownloadHandler : IRequestHandler<OwnerDownloadQuery, BaseResponse<OwnerDownloadResult>>
{
    private readonly IStorageObjectRepository _repo;
    private readonly IStorageService          _storage;

    public OwnerDownloadHandler(IStorageObjectRepository repo, IStorageService storage)
    {
        _repo    = repo;
        _storage = storage;
    }

    public async Task<BaseResponse<OwnerDownloadResult>> Handle(
        OwnerDownloadQuery request, CancellationToken ct)
    {
        var obj = await _repo.GetByIdAsync(request.FileId);
        if (obj == null)
            return BaseResponse<OwnerDownloadResult>.Fail("Файл не найден");
        if (obj.OwnerId != request.ShopId)
            return BaseResponse<OwnerDownloadResult>.Fail("Нет доступа");

        var contentDisposition = BuildContentDisposition(obj.FileName);
        var url = _storage.GeneratePresignedUrl(obj.Key, 60, contentDisposition);

        return BaseResponse<OwnerDownloadResult>.Success(
            new OwnerDownloadResult(url, obj.FileName));
    }

    internal static string BuildContentDisposition(string fileName)
    {
        var safe    = Regex.Replace(fileName, @"[^\x20-\x7E]", "_").Replace('"', '_');
        var encoded = Uri.EscapeDataString(fileName);
        return $"attachment; filename=\"{safe}\"; filename*=UTF-8''{encoded}";
    }
}

// ── Bulk (manifest) ────────────────────────────────────────────────────────────

public record BulkOwnerDownloadQuery(List<Guid> FileIds, Guid ShopId)
    : IRequest<BaseResponse<List<OwnerDownloadResult>>>;

public class BulkOwnerDownloadHandler
    : IRequestHandler<BulkOwnerDownloadQuery, BaseResponse<List<OwnerDownloadResult>>>
{
    private readonly IStorageObjectRepository _repo;
    private readonly IStorageService          _storage;

    public BulkOwnerDownloadHandler(IStorageObjectRepository repo, IStorageService storage)
    {
        _repo    = repo;
        _storage = storage;
    }

    public async Task<BaseResponse<List<OwnerDownloadResult>>> Handle(
        BulkOwnerDownloadQuery request, CancellationToken ct)
    {
        if (request.FileIds.Count == 0)
            return BaseResponse<List<OwnerDownloadResult>>.Fail("Список файлов пуст");
        if (request.FileIds.Count > 500)
            return BaseResponse<List<OwnerDownloadResult>>.Fail("Максимум 500 файлов за раз");

        var objects = await _repo.GetByIdsAsync(request.FileIds);

        // Security: only files belonging to this shop
        var results = objects
            .Where(o => o.OwnerId == request.ShopId)
            .Select(o =>
            {
                var cd  = OwnerDownloadHandler.BuildContentDisposition(o.FileName);
                var url = _storage.GeneratePresignedUrl(o.Key, 120, cd); // 2-hour TTL for bulk
                return new OwnerDownloadResult(url, o.FileName);
            })
            .ToList();

        return BaseResponse<List<OwnerDownloadResult>>.Success(results);
    }
}

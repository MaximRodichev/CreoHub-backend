using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IMediaProductRepository : IRepository<MediaProduct, (Guid product, Guid storageObject)>
{
    public Task<string> GetPreviewKeyByProductId(int productId);
    public Task<Dictionary<int, string?>> GetPreviewKeysByProductIds(IEnumerable<int> productIds);
    public Task<Dictionary<int, (string? Key, string? ThumbnailKey)>> GetPreviewKeysWithThumbnailByProductIds(IEnumerable<int> productIds);
    /// <summary>Найти MediaProduct по StorageObjectId (нужно для ThumbnailGenerationService).</summary>
    public Task<MediaProduct?> GetByStorageObjectIdAsync(Guid storageObjectId);
    /// <summary>Все MediaProduct без ThumbnailId где StorageObject — видео. Для массовой генерации (один шоп).</summary>
    public Task<List<MediaProduct>> GetVideosWithoutThumbnailAsync(Guid shopId);

    /// <summary>Все MediaProduct без ThumbnailId где StorageObject — видео. Для ночного бэкфилла по всей платформе.</summary>
    public Task<List<MediaProduct>> GetAllVideosWithoutThumbnailAsync();
    /// <summary>Найти MediaProduct по (productId, storageObjectId) для изменения SortOrder.</summary>
    public Task<MediaProduct?> GetByProductAndStorageAsync(int productId, Guid storageObjectId);

}
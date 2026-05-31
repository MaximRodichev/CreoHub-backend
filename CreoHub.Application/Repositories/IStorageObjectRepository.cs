using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IStorageObjectRepository : IRepository<StorageObject, Guid>
{
    /// <summary>
    /// Пагинированный список файлов шопа с сервер-сайд фильтрацией и сортировкой.
    /// </summary>
    Task<StorageObjectsPagedResponseDTO> GetPagedByShopIdAsync(
        Guid    shopId,
        int     page,
        int     pageSize,
        string? search,  // ILIKE по FileName
        string? type,    // "image" | "video" | "other" | null = все
        string  sort,    // "date" | "name" | "size"
        string  order    // "asc" | "desc"
    );

    /// <summary>
    /// Возвращает все файлы со статусом оптимизации Queued или Processing.
    /// Используется при старте сервера для восстановления потерянных задач.
    /// </summary>
    Task<List<StorageObject>> GetStuckOptimizationJobsAsync(CancellationToken ct = default);

    /// <summary>
    /// Все ключи хранилища, зарегистрированные в БД.
    /// Используется для поиска orphan-файлов в R2.
    /// </summary>
    Task<HashSet<string>> GetAllKeysSetAsync(CancellationToken ct = default);
}
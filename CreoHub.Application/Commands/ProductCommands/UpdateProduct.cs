using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

// ── Snapshot model (serialised to JSON, stored in ProductEditHistory) ──────────
namespace CreoHub.Application.Commands.ProductCommands;

public record ProductSnapshotFile(string Id, string Name, int PriceWeight);
public record ProductSnapshot(
    string         Name,
    string?        Description,
    decimal        Price,
    List<string>   Tags,
    List<ProductSnapshotFile> Files);

public record UpdateProductCommand(Guid shopId, UpdateProductInfoDTO dto) : IRequest<BaseResponse<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IProductEditHistoryRepository _editHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITagRepository _tagRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IStorageService _storageService;

    public UpdateProductHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IProductEditHistoryRepository editHistoryRepository,
        IUnitOfWork unitOfWork,
        ITagRepository tagRepository,
        IPriceRepository priceRepository,
        IStorageObjectRepository storageObjectRepository,
        IStorageService storageService)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _editHistoryRepository = editHistoryRepository;
        _unitOfWork          = unitOfWork;
        _tagRepository       = tagRepository;
        _priceRepository     = priceRepository;
        _storageObjectRepository = storageObjectRepository;
        _storageService      = storageService;
    }
    
    public async Task<BaseResponse<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productRepository.GetProductById(request.dto.Id);
            if (response == null)
                return BaseResponse<bool>.Fail($"Product with id {request.dto.Id} not found.");

            // ── Проверка прав доступа ──────────────────────────────────────────────
            if (response.OwnerId != request.shopId)
                return BaseResponse<bool>.Fail("Access denied: product does not belong to this shop.");

            // ── Снимок состояния ДО изменений ──────────────────────────────────────
            var preEditSnapshot = new ProductSnapshot(
                Name:        response.Name,
                Description: response.Description,
                Price:       response.Prices.OrderByDescending(p => p.Date).FirstOrDefault()?.Value ?? 0m,
                Tags:        response.Tags.Select(t => t.Name).ToList(),
                Files:       response.ContentFiles
                                 .Where(f => !f.IsArchived)
                                 .Select(f => new ProductSnapshotFile(
                                     f.Id.ToString(), f.PreviewName, f.PriceWeight))
                                 .ToList()
            );
            await _editHistoryRepository.AddAsync(
                new ProductEditHistory(response.Id, request.shopId,
                    JsonSerializer.Serialize(preEditSnapshot)),
                cancellationToken);
            // ── End snapshot ────────────────────────────────────────────────────────

            if (response.Name != request.dto.Name)
            {
                response.UpdateName(request.dto.Name);
            }

            // null — поле не передали, slug не трогаем (старые ссылки стабильны).
            // Непустое значение — явная смена URL владельцем.
            if (!string.IsNullOrWhiteSpace(request.dto.Slug) && request.dto.Slug != response.Slug)
            {
                response.UpdateSlug(request.dto.Slug);
            }

            if (response.Description != request.dto.Description)
            {
                response.UpdateDescription(request.dto.Description);
            }

            if (response.Prices.OrderBy(x=>x.Date).Last().Value != request.dto.Price)
            {
                var newPrice = new Price(request.dto.Price, request.dto.Id);
                
                await _priceRepository.AddAsync(newPrice);
            }

            if (request.dto.ObjectStorageIds != null)
            {
                var existingIds = response.MediaProducts
                    .Select(x => x.StorageObjectId)
                    .ToHashSet();

                var incomingIds = request.dto.ObjectStorageIds.ToHashSet();

                // Удалить исчезнувшие
                var toRemove = response.MediaProducts
                    .Where(x => !incomingIds.Contains(x.StorageObjectId))
                    .ToList();

                foreach (var media in toRemove)
                {
                    var storageObject = await _storageObjectRepository.GetByIdAsync(media.StorageObjectId);
                    storageObject.ChangeFileType(FileType.Unregistred);

                    // Delete associated thumbnail from storage and DB
                    if (media.ThumbnailId.HasValue)
                    {
                        var thumb = await _storageObjectRepository.GetByIdAsync(media.ThumbnailId.Value);
                        if (thumb != null)
                        {
                            _storageObjectRepository.Remove(thumb);
                            _ = _storageService.DeleteFileAsync(thumb.Key); // fire-and-forget
                        }
                    }

                    response.RemoveMedia(media);
                }

                // Добавить новые
                var toAdd = incomingIds.Where(x => !existingIds.Contains(x)).ToList();

                foreach (var storageObjectId in toAdd)
                {
                    var storageObject = await _storageObjectRepository.GetByIdAsync(storageObjectId);
                    if (storageObject == null) continue;

                    // Запрещаем прикреплять чужие файлы
                    if (storageObject.OwnerId != request.shopId)
                        return BaseResponse<bool>.Fail(
                            $"Access denied: storage object {storageObjectId} does not belong to this shop.");

                    storageObject.ChangeFileType(FileType.Media);
                    _storageObjectRepository.Update(storageObject); // AsNoTracking → explicit Update required

                    response.AddMedia(new MediaProduct(
                        response.Id,
                        storageObject.Id,
                        0));
                }

                // Обновить порядок сортировки согласно переданному списку
                for (int i = 0; i < request.dto.ObjectStorageIds.Count; i++)
                {
                    var mediaItem = response.MediaProducts
                        .FirstOrDefault(m => m.StorageObjectId == request.dto.ObjectStorageIds[i]);
                    mediaItem?.UpdateSortOrder(i);
                }
            }
            var incomingTagIds = request.dto.Tags.ToHashSet();
            var existingTagIds = response.Tags.Select(x => x.Name).ToHashSet();
            if (!incomingTagIds.SetEquals(existingTagIds))
            {
                response.ReplaceTags(await _tagRepository.GetByNamesAsync(request.dto.Tags));
            }
            
            // Любое редактирование → OnModerating (кроме Banned/Archived)
            var oldStatus = response.ProductStatus;
            if (oldStatus != ProductStatus.Banned && oldStatus != ProductStatus.Archived
                && oldStatus != ProductStatus.OnModerating)
            {
                response.SendToModeration();
                await _statusLogRepository.AddAsync(new ProductStatusLog(
                    response.Id, oldStatus, ProductStatus.OnModerating,
                    reason: "Отправлен на модерацию после редактирования"),
                    cancellationToken);
            }

            _productRepository.Update(response);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex) when (ExceptionChainContains(ex, "IX_Products_Slug"))
        {
            return BaseResponse<bool>.Fail("Товар с таким URL (slug) уже существует. Укажите другой URL.");
        }
        catch (Exception ex) when (ExceptionChainContains(ex, "IX_Products_Name"))
        {
            return BaseResponse<bool>.Fail("Товар с таким названием уже существует. Выберите другое название.");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Walks the full InnerException chain checking every message —
    /// needed because the UoW may wrap DbUpdateException in a custom exception.
    /// </summary>
    private static bool ExceptionChainContains(Exception? ex, string text)
    {
        while (ex is not null)
        {
            if (ex.Message.Contains(text)) return true;
            ex = ex.InnerException;
        }
        return false;
    }

}
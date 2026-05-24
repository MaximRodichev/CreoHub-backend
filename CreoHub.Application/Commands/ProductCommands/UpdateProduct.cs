using System.Threading;
using System.Threading.Tasks;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record UpdateProductCommand(Guid shopId, UpdateProductInfoDTO dto) : IRequest<BaseResponse<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITagRepository _tagRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IStorageService _storageService;

    public UpdateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork,  ITagRepository tagRepository, IPriceRepository priceRepository, IStorageObjectRepository storageObjectRepository, IStorageService storageService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _tagRepository = tagRepository;
        _priceRepository = priceRepository;
        _storageObjectRepository = storageObjectRepository;
        _storageService = storageService;
    }
    
    public async Task<BaseResponse<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productRepository.GetProductById(request.dto.Id);
            if (response == null)
            {
                return BaseResponse<bool>.Fail($"Product with id {request.dto.Id} not found.");
            }

            if (response.Name != request.dto.Name)
            {
                response.UpdateName(request.dto.Name);
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
            
            _productRepository.Update(response);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
    
}
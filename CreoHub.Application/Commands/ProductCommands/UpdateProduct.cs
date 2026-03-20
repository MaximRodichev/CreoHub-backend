using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record UpdateProductCommand(Guid shopId, UpdateProductInfoDTO dto) : IRequest<BaseResponse<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, BaseResponse<bool>>
{
    IProductRepository _productRepository;
    IUnitOfWork _unitOfWork;
    ITagRepository _tagRepository;
    IPriceRepository _priceRepository;
    IStorageObjectRepository _storageObjectRepository;
    
    public UpdateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork,  ITagRepository tagRepository, IPriceRepository priceRepository, IStorageObjectRepository storageObjectRepository)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _tagRepository = tagRepository;
        _priceRepository = priceRepository;
        _storageObjectRepository = storageObjectRepository;
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
                response.Name = request.dto.Name;
            }

            if (response.Description != request.dto.Description)
            {
                response.Description = request.dto.Description;
            }

            if (response.Prices.OrderBy(x=>x.Date).Last().Value != request.dto.Price)
            {
                var newPrice = new Price()
                {
                    Value = request.dto.Price,
                    ProductId = request.dto.Id
                };
                
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
                    response.MediaProducts.Remove(media);
                }

                // Добавить новые
                var toAdd = incomingIds.Where(x => !existingIds.Contains(x)).ToList();

                foreach (var storageObjectId in toAdd)
                {
                    var storageObject = await _storageObjectRepository.GetByIdAsync(storageObjectId);
                    storageObject.ChangeFileType(FileType.Media);

                    response.MediaProducts.Add(new MediaProduct
                    {
                        ProductId = response.Id,
                        StorageObject = storageObject,
                        ThumbnailId = null,
                        SortOrder = (response.MediaProducts.Count + 1) * 10
                    });
                }
            }
            var incomingTagIds = request.dto.Tags.Select(x => x).ToHashSet();
            var existingTagIds = response.Tags.Select(x => x.Name).ToHashSet();
            if (!incomingTagIds.SetEquals(existingTagIds))
            {
                response.Tags = await _tagRepository.GetByNamesAsync(request.dto.Tags);
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
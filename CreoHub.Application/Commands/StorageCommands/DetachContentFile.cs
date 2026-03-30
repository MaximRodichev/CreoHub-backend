using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record DetachContentFileCommand(Guid ShopId, DetachContentFileDTO Dto) : IRequest<BaseResponse<bool>>;

public class DetachContentFileHandler : IRequestHandler<DetachContentFileCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    
    public DetachContentFileHandler(IStorageObjectRepository storageObjectRepository, IUnitOfWork unitOfWork,  IContentFileRepository contentFileRepository, IProductRepository productRepository)
    {
        _unitOfWork = unitOfWork;
        _contentFileRepository = contentFileRepository;
        _productRepository = productRepository;
        _storageObjectRepository = storageObjectRepository;
    }
    
    
    public async Task<BaseResponse<bool>> Handle(DetachContentFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Product? product = await _productRepository.GetByIdAsync(request.Dto.ProductId);
            if (product == null)
                return BaseResponse<bool>.Fail("Product not found");
            if(product.OwnerId != request.ShopId)
                return BaseResponse<bool>.Fail("You cannot edit the content file");

            var contentFile = await _contentFileRepository.GetByIdAsync(request.Dto.ContentFileId);
            if (contentFile == null)
                return BaseResponse<bool>.Fail("Content file not found");
            if(contentFile.ProductId != request.Dto.ProductId)
                return BaseResponse<bool>.Fail("Content file not included into this Product");

            StorageObject? storageObject = await _storageObjectRepository.GetByIdAsync(contentFile.StorageObjectId);
            if (storageObject == null)
                return BaseResponse<bool>.Fail("Storage object not found");
            if (storageObject.OwnerId != request.ShopId)
                return BaseResponse<bool>.Fail("You cannot edit the content file");
            
            List<ContentFile> contentFiles = await _contentFileRepository.GetByStorageObjectIdAsync(storageObject.Id);
            if (contentFiles.Count == 1)
            {
                storageObject.ChangeFileType(FileType.Unregistred);
            }
            
            product.RemoveContentFile(contentFile);
            _contentFileRepository.Remove(contentFile);
            _productRepository.Update(product);
            _storageObjectRepository.Update(storageObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
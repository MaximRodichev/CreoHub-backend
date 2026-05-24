using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record AttachContentFileCommand(Guid ShopId, AttachContentFileDTO Dto) : IRequest<BaseResponse<ContentFileInfo>>;

public class AttachContentFileHandler : IRequestHandler<AttachContentFileCommand, BaseResponse<ContentFileInfo>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly  IContentFileRepository _contentFileRepository;
    

    public AttachContentFileHandler(IContentFileRepository contentFileRepository, IProductRepository productRepository, IUnitOfWork unitOfWork,  IStorageObjectRepository storageObjectRepository)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _storageObjectRepository = storageObjectRepository;
        _contentFileRepository = contentFileRepository;
    }
    
    public async Task<BaseResponse<ContentFileInfo>> Handle(AttachContentFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Product? product = await _productRepository.GetByIdAsync(request.Dto.ProductId);
            if (product == null)
                return BaseResponse<ContentFileInfo>.Fail("Товар не найден.");
            if (product.OwnerId != request.ShopId)
                return BaseResponse<ContentFileInfo>.Fail("У вас нет прав для редактирования этого товара");

            StorageObject? storageObject = await _storageObjectRepository.GetByIdAsync(request.Dto.StorageObjectId);
            if (storageObject == null)
                return BaseResponse<ContentFileInfo>.Fail("Not found StorageObject");

            storageObject.ChangeFileType(FileType.Content);

            ContentFile contentFile = new ContentFile(
                request.Dto.PriceWeight,
                request.Dto.PreviewName,
                request.Dto.StorageObjectId,
                request.Dto.ProductId);

            product.AddContentFile(contentFile);
            _storageObjectRepository.Update(storageObject);
            await _contentFileRepository.AddAsync(contentFile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<ContentFileInfo>.Success(new ContentFileInfo
            {
                Id              = contentFile.Id,
                StorageObjectId = contentFile.StorageObjectId,
                PreviewName     = contentFile.PreviewName,
                PriceWeight     = contentFile.PriceWeight,
                StorageFileName = storageObject.FileName,
                IsArchived      = false,
            });
        }
        catch (Exception ex) when (ExceptionChainContains(ex, "IX_ContentFiles_ProductId_StorageObjectId"))
        {
            return BaseResponse<ContentFileInfo>.Fail("Этот файл уже прикреплён к данному товару.");
        }
        catch (Exception ex)
        {
            return BaseResponse<ContentFileInfo>.Fail(ex.Message);
        }
    }

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
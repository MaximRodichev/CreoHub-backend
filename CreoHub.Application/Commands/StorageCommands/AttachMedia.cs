using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record AttachMedia(Guid shopId, int productId, Guid storageObjectId) : IRequest<BaseResponse<bool>>;

public class AttachMediaHandler : IRequestHandler<AttachMedia, BaseResponse<bool>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMediaProductRepository _mediaProductRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public AttachMediaHandler(IUnitOfWork unitOfWork, IStorageObjectRepository storageObjectRepository,  IMediaProductRepository mediaProductRepository, IProductRepository productRepository)
    {
        _unitOfWork = unitOfWork;
        _storageObjectRepository = storageObjectRepository;
        _mediaProductRepository = mediaProductRepository;
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<bool>> Handle(AttachMedia request, CancellationToken cancellationToken)
    {
        try
        {
            var storageObject = await _storageObjectRepository.GetByIdAsync(request.storageObjectId);
            var productObject = await _productRepository.GetByIdAsync(request.productId);
            
            if (storageObject == null)
                return BaseResponse<bool>.Fail("Файл не найден");
            if (storageObject.MediaProduct != null)
                return BaseResponse<bool>.Fail("Файл уже привязан к продукту");
            if (storageObject.OwnerId != request.shopId)
                return BaseResponse<bool>.Fail("Этот файл не принадлежит вам, ошибка прав доступа.");
            if (productObject == null)
                return BaseResponse<bool>.Fail("Продукт не был найден");
            if (productObject.OwnerId != storageObject.OwnerId)
                return BaseResponse<bool>.Fail("Этот продукт не принадлежит файл, ошибка прав доступа");

            MediaProduct mediaProduct = new MediaProduct(productObject.Id, storageObject.Id, 0);
            storageObject.ChangeFileType(FileType.Media);
            await _mediaProductRepository.AddAsync(mediaProduct);
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
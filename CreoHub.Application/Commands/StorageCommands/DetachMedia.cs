using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record DetachMedia(Guid shopId, Guid storageObjectId) : IRequest<BaseResponse<bool>>;

public class DetachMediaHandler : IRequestHandler<DetachMedia, BaseResponse<bool>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IMediaProductRepository _mediaProductRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public DetachMediaHandler(IUnitOfWork unitOfWork, IStorageObjectRepository storageObjectRepository,  IMediaProductRepository mediaProductRepository)
    {
        _unitOfWork = unitOfWork;
        _storageObjectRepository = storageObjectRepository;
        _mediaProductRepository = mediaProductRepository;
        
    }
    
    public async Task<BaseResponse<bool>> Handle(DetachMedia request, CancellationToken cancellationToken)
    {
        try
        {
            var storageObject = await _storageObjectRepository.GetByIdAsync(request.storageObjectId);
            if (storageObject == null)
                return BaseResponse<bool>.Fail("Файл не найден");
            if (storageObject.MediaProduct == null)
            {
                return BaseResponse<bool>.Fail("Ошибка, не был найден медиа связанный объект");
            }
            if (storageObject.OwnerId != request.shopId)
            {
                return BaseResponse<bool>.Fail("Этот продукт не принадлежит вам, ошибка прав доступа.");
            }
            _mediaProductRepository.Remove(storageObject.MediaProduct);
            storageObject.ChangeFileType(FileType.Unregistred);
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
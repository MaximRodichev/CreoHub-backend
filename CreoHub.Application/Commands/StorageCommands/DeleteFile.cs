using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record DeleteFile(Guid id, Guid shopId) : IRequest<BaseResponse<bool>>;

public class DeleteFileHandler : IRequestHandler<DeleteFile, BaseResponse<bool>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFileHandler(IStorageObjectRepository storageObjectRepository, IUnitOfWork unitOfWork, IStorageService storageService)
    {
        _storageObjectRepository = storageObjectRepository;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
    }
    
    public async Task<BaseResponse<bool>> Handle(DeleteFile request, CancellationToken cancellationToken)
    {
        try
        {
            var storageObject = await _storageObjectRepository.GetByIdAsync(request.id);
            if (storageObject.FileType != FileType.Unregistred)
            {
                return BaseResponse<bool>.Fail("Файл используется в качестве контента или медиа. Отвяжите перед удалением");
            }
            if (storageObject.OwnerId != request.shopId)
            {
                return BaseResponse<bool>.Fail("Вы не являетесь владельцем данного файла");
            }
            _storageObjectRepository.Remove(storageObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _storageService.DeleteFileAsync(storageObject.Key);
            
            return BaseResponse<bool>.Success(true);
        }
        catch(Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
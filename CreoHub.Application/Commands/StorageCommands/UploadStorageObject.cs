using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;



public record UploadStorageObjectCommand(Stream FileStream, string FileName, string FileType, Guid shopId) : IRequest<BaseResponse<StorageObject>>;

public class UploadStorageObjectHandler : IRequestHandler<UploadStorageObjectCommand, BaseResponse<StorageObject>>
{
    private readonly IStorageObjectRepository _objectRepository;
    private readonly IStorageService _storageService;
    
    private readonly IUnitOfWork _unitOfWork;

    public UploadStorageObjectHandler(IStorageObjectRepository objectRepository, IStorageService storageService, IUnitOfWork unitOfWork)
    {
        _objectRepository = objectRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
    }
    
    
    public async Task<BaseResponse<StorageObject>> Handle(UploadStorageObjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            
            var key = $"{request.shopId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";
            var fileSize = request.FileStream.Length;
            
            await _storageService.UploadFileAsync(request.FileStream, key, request.FileType);
            
            var fileObject = new StorageObject()
            {
                FileSize = fileSize,
                Key = key,
                MimeType = request.FileType,
                OwnerId = request.shopId,
                FileName = request.FileName,
            };
            
            var response = await _objectRepository.AddAsync(fileObject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<StorageObject>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<StorageObject>.Fail(ex.Message);
        }
    }
}
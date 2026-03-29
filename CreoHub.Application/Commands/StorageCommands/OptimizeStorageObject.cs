using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record OptimizeStorageObjectCommand(Guid storageObjectId, Guid shopId) 
    : IRequest<BaseResponse<bool>>;

public class OptimizeStorageObjectHandler : IRequestHandler<OptimizeStorageObjectCommand, BaseResponse<bool>>
{
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IVideoOptimizationQueueService _queue;

    public OptimizeStorageObjectHandler(
        IStorageObjectRepository storageObjectRepository,
        IVideoOptimizationQueueService queue)
    {
        _storageObjectRepository = storageObjectRepository;
        _queue = queue;
    }

    public async Task<BaseResponse<bool>> Handle(OptimizeStorageObjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var storageObject = await _storageObjectRepository.GetByIdAsync(request.storageObjectId);

            if (storageObject == null)
                return BaseResponse<bool>.Fail("Файл не найден");

            if (storageObject.OwnerId != request.shopId)
                return BaseResponse<bool>.Fail("Файл не принадлежит вам");

            if (storageObject.MimeType != "video/mp4")
                return BaseResponse<bool>.Fail("Файл не является mp4");

            if (storageObject.FileSize <= 5 * 1024 * 1024)
                return BaseResponse<bool>.Fail("Файл меньше 5MB, оптимизация не нужна");

            if (storageObject.FileType == FileType.Content)
                return BaseResponse<bool>.Fail("Файл контента запрещено понижать в качестве, отвяжите от продукта перед сжатием");

            _queue.Enqueue(request.storageObjectId);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record DetachContentFileResult(bool Archived, string Message);

public record DetachContentFileCommand(Guid ShopId, DetachContentFileDTO Dto)
    : IRequest<BaseResponse<DetachContentFileResult>>;

public class DetachContentFileHandler
    : IRequestHandler<DetachContentFileCommand, BaseResponse<DetachContentFileResult>>
{
    private readonly IUnitOfWork                 _unitOfWork;
    private readonly IContentFileRepository      _contentFileRepository;
    private readonly IProductRepository          _productRepository;
    private readonly IStorageObjectRepository    _storageObjectRepository;

    public DetachContentFileHandler(
        IStorageObjectRepository storageObjectRepository,
        IUnitOfWork unitOfWork,
        IContentFileRepository contentFileRepository,
        IProductRepository productRepository)
    {
        _unitOfWork              = unitOfWork;
        _contentFileRepository   = contentFileRepository;
        _productRepository       = productRepository;
        _storageObjectRepository = storageObjectRepository;
    }

    public async Task<BaseResponse<DetachContentFileResult>> Handle(
        DetachContentFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(request.Dto.ProductId);
            if (product == null)
                return BaseResponse<DetachContentFileResult>.Fail("Product not found");
            if (product.OwnerId != request.ShopId)
                return BaseResponse<DetachContentFileResult>.Fail("You cannot edit the content file");

            var contentFile = await _contentFileRepository.GetByIdAsync(request.Dto.ContentFileId);
            if (contentFile == null)
                return BaseResponse<DetachContentFileResult>.Fail("Content file not found");
            if (contentFile.ProductId != request.Dto.ProductId)
                return BaseResponse<DetachContentFileResult>.Fail("Content file not included into this Product");

            // Нельзя открепить единственный активный контент-файл — товар не может быть без контента
            var activeCount = await _contentFileRepository.GetActiveCountByProductIdAsync(request.Dto.ProductId);
            if (activeCount <= 1)
                return BaseResponse<DetachContentFileResult>.Fail("Нельзя открепить единственный контент-файл товара. Сначала добавьте другой файл.");

            var storageObject = await _storageObjectRepository.GetByIdAsync(contentFile.StorageObjectId);
            if (storageObject == null)
                return BaseResponse<DetachContentFileResult>.Fail("Storage object not found");
            if (storageObject.OwnerId != request.ShopId)
                return BaseResponse<DetachContentFileResult>.Fail("You cannot edit the content file");

            var hasPurchases = await _contentFileRepository.HasPurchasesAsync(contentFile.Id);

            if (hasPurchases)
            {
                // ── Кейс: файл куплен → архивируем, не удаляем ────────────────
                contentFile.Archive();
                storageObject.Lock();
                // FileType остаётся Content — файл физически всё ещё контент файл

                _contentFileRepository.Update(contentFile);
                _storageObjectRepository.Update(storageObject);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return BaseResponse<DetachContentFileResult>.Success(
                    new DetachContentFileResult(
                        Archived: true,
                        Message: "Файл архивирован. Существующие покупатели сохраняют доступ."));
            }
            else
            {
                // ── Кейс: никто не купил → чистое удаление ───────────────────
                var contentFiles = await _contentFileRepository.GetByStorageObjectIdAsync(storageObject.Id);
                if (contentFiles.Count == 1)
                    storageObject.ChangeFileType(FileType.Unregistred);

                product.RemoveContentFile(contentFile);
                _contentFileRepository.Remove(contentFile);
                _productRepository.Update(product);
                _storageObjectRepository.Update(storageObject);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return BaseResponse<DetachContentFileResult>.Success(
                    new DetachContentFileResult(
                        Archived: false,
                        Message: "Файл успешно отвязан от товара."));
            }
        }
        catch (Exception ex)
        {
            return BaseResponse<DetachContentFileResult>.Fail(ex.Message);
        }
    }
}
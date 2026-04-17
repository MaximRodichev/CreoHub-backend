using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record UpdateContentFileCommand(Guid ShopId, Guid ContentFileId, UpdateContentFileDTO Dto)
    : IRequest<BaseResponse<bool>>;

public class UpdateContentFileHandler
    : IRequestHandler<UpdateContentFileCommand, BaseResponse<bool>>
{
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateContentFileHandler(
        IContentFileRepository contentFileRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _contentFileRepository = contentFileRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        UpdateContentFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _contentFileRepository.GetByIdAsync(request.ContentFileId);
            if (file is null)
                return BaseResponse<bool>.Fail("Content file not found.");

            // Проверяем, что файл принадлежит продукту этого шопа
            var shopId = await _productRepository.GetShopIdByProductId(file.ProductId);
            if (shopId != request.ShopId)
                return BaseResponse<bool>.Fail("Access denied.");

            if (request.Dto.PreviewName is not null)
                file.UpdatePreviewName(request.Dto.PreviewName);

            if (request.Dto.PriceWeight.HasValue)
                file.UpdatePriceWeight(request.Dto.PriceWeight.Value);

            _contentFileRepository.Update(file);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

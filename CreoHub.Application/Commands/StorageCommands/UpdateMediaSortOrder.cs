using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.StorageCommands;

public record UpdateMediaSortOrderCommand(Guid ShopId, int ProductId, Guid StorageObjectId, int SortOrder)
    : IRequest<BaseResponse<bool>>;

public class UpdateMediaSortOrderHandler
    : IRequestHandler<UpdateMediaSortOrderCommand, BaseResponse<bool>>
{
    private readonly IMediaProductRepository _mediaProductRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMediaSortOrderHandler(
        IMediaProductRepository mediaProductRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _mediaProductRepository = mediaProductRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        UpdateMediaSortOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Проверяем владельца продукта
            var shopId = await _productRepository.GetShopIdByProductId(request.ProductId);
            if (shopId != request.ShopId)
                return BaseResponse<bool>.Fail("Access denied.");

            var mediaProduct = await _mediaProductRepository
                .GetByProductAndStorageAsync(request.ProductId, request.StorageObjectId);

            if (mediaProduct is null)
                return BaseResponse<bool>.Fail("Media not found for this product.");

            mediaProduct.UpdateSortOrder(request.SortOrder);
            _mediaProductRepository.Update(mediaProduct);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

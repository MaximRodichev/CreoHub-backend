using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record ChangeProductStatusCommand(Guid ShopId, int ProductId, string TargetStatus, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

public class ChangeProductStatusHandler
    : IRequestHandler<ChangeProductStatusCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProductStatusHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository    = productRepository;
        _statusLogRepository  = statusLogRepository;
        _unitOfWork           = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        ChangeProductStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null)
                return BaseResponse<bool>.Fail("Product not found.");

            if (product.OwnerId != request.ShopId)
                return BaseResponse<bool>.Fail("Access denied.");

            // Продавец не может менять статус забаненного товара
            if (product.ProductStatus == ProductStatus.Banned)
                return BaseResponse<bool>.Fail("Забаненный товар не может быть изменён продавцом.");

            var oldStatus = product.ProductStatus;

            switch (request.TargetStatus?.ToLower())
            {
                case "active":
                    product.Activate();
                    break;
                case "hidden":
                    product.Hide();
                    break;
                case "onmoderating":
                    product.SendToModeration();
                    break;
                default:
                    return BaseResponse<bool>.Fail("Invalid status. Use 'Active', 'Hidden' or 'OnModerating'.");
            }

            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, product.ProductStatus,
                reason: request.Reason,
                changedById: null), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

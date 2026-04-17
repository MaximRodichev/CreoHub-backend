using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record ChangeProductStatusCommand(Guid ShopId, int ProductId, string TargetStatus)
    : IRequest<BaseResponse<bool>>;

public class ChangeProductStatusHandler
    : IRequestHandler<ChangeProductStatusCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProductStatusHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
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

            switch (request.TargetStatus?.ToLower())
            {
                case "active":
                    product.Activate();
                    break;
                case "hidden":
                    product.Hide();
                    break;
                default:
                    return BaseResponse<bool>.Fail("Invalid status. Use 'Active' or 'Hidden'.");
            }

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

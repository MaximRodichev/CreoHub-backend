using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record DeleteProductCommand(Guid ShopId, int ProductId) : IRequest<BaseResponse<bool>>;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null)
                return BaseResponse<bool>.Fail("Product not found.");

            if (product.OwnerId != request.ShopId)
                return BaseResponse<bool>.Fail("Access denied.");

            // Soft delete: Hidden — продукт скрыт для покупателей, но остаётся в базе
            // Если уже Hidden — ничего не делаем
            if (product.ProductStatus == ProductStatus.Active)
                product.Hide();

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

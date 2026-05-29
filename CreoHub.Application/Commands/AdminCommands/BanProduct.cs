using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

public record BanProductCommand(int ProductId, string Reason, Guid AdminUserId)
    : IRequest<BaseResponse<bool>>;

public record UnbanProductCommand(int ProductId, Guid AdminUserId)
    : IRequest<BaseResponse<bool>>;

public class BanProductHandler : IRequestHandler<BanProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BanProductHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(BanProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Product not found.");

            if (product.ProductStatus == ProductStatus.Banned)
                return BaseResponse<bool>.Fail("Товар уже заблокирован.");

            var oldStatus = product.ProductStatus;
            product.Ban(request.Reason);
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.Banned,
                reason: request.Reason,
                changedById: request.AdminUserId), ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

public class UnbanProductHandler : IRequestHandler<UnbanProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnbanProductHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(UnbanProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Product not found.");

            var oldStatus = product.ProductStatus;
            product.Unban();
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, product.ProductStatus,
                reason: "Бан снят администратором",
                changedById: request.AdminUserId), ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

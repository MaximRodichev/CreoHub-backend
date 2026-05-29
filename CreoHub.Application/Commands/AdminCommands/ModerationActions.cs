using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

public record ApproveModerationCommand(int ProductId, Guid AdminUserId)
    : IRequest<BaseResponse<bool>>;

public record RejectModerationCommand(int ProductId, Guid AdminUserId, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

// ── Approve ─────────────────────────────────────────────────────────────────

public class ApproveModerationHandler : IRequestHandler<ApproveModerationCommand, BaseResponse<bool>>
{
    private readonly IProductRepository         _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork                _unitOfWork;

    public ApproveModerationHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(ApproveModerationCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Product not found.");

            var oldStatus = product.ProductStatus;
            product.ApproveModeration();
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.Active,
                reason: "Модерация пройдена — одобрено администратором",
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

// ── Reject ──────────────────────────────────────────────────────────────────

public class RejectModerationHandler : IRequestHandler<RejectModerationCommand, BaseResponse<bool>>
{
    private readonly IProductRepository         _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork                _unitOfWork;

    public RejectModerationHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(RejectModerationCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Product not found.");

            var oldStatus = product.ProductStatus;
            product.RejectModeration();
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.ModerationFailed,
                reason: string.IsNullOrWhiteSpace(request.Reason)
                    ? "Отклонено администратором"
                    : request.Reason,
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

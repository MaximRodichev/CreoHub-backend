using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

// ── AdminHide ────────────────────────────────────────────────────────────────

public record AdminHideProductCommand(int ProductId, Guid AdminUserId, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

public class AdminHideProductHandler : IRequestHandler<AdminHideProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository          _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork                 _unitOfWork;

    public AdminHideProductHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(AdminHideProductCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Товар не найден.");

            var oldStatus = product.ProductStatus;

            if (oldStatus == ProductStatus.Hidden)
                return BaseResponse<bool>.Fail("Товар уже скрыт.");

            product.AdminHide();
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.Hidden,
                reason: string.IsNullOrWhiteSpace(request.Reason)
                    ? "Скрыто администратором"
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

// ── AdminSendToModeration ────────────────────────────────────────────────────

public record AdminSendToModerationCommand(int ProductId, Guid AdminUserId, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

public class AdminSendToModerationHandler : IRequestHandler<AdminSendToModerationCommand, BaseResponse<bool>>
{
    private readonly IProductRepository          _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IUnitOfWork                 _unitOfWork;

    public AdminSendToModerationHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository   = productRepository;
        _statusLogRepository = statusLogRepository;
        _unitOfWork          = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(AdminSendToModerationCommand request, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetProductById(request.ProductId);
            if (product is null) return BaseResponse<bool>.Fail("Товар не найден.");

            var oldStatus = product.ProductStatus;

            if (oldStatus == ProductStatus.OnModerating)
                return BaseResponse<bool>.Fail("Товар уже находится на модерации.");

            product.AdminSendToModeration();
            _productRepository.Update(product);

            await _statusLogRepository.AddAsync(new ProductStatusLog(
                product.Id, oldStatus, ProductStatus.OnModerating,
                reason: string.IsNullOrWhiteSpace(request.Reason)
                    ? "Отправлено на модерацию администратором"
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

using CreoHub.Application.Commands.ShopFollows;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Application.Commands.ProductCommands;

public record ChangeProductStatusCommand(Guid ShopId, int ProductId, string TargetStatus, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

public class ChangeProductStatusHandler
    : IRequestHandler<ChangeProductStatusCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductStatusLogRepository _statusLogRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProductStatusHandler(
        IProductRepository productRepository,
        IProductStatusLogRepository statusLogRepository,
        IServiceScopeFactory scopeFactory,
        IUnitOfWork unitOfWork)
    {
        _productRepository    = productRepository;
        _statusLogRepository  = statusLogRepository;
        _scopeFactory         = scopeFactory;
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

            // Публиковался ли товар когда-либо (для «нового товара» только при первой публикации).
            var everPublished = (await _statusLogRepository.GetByProductIdAsync(product.Id, cancellationToken)
                                 ?? new List<ProductStatusLog>())
                .Any(l => l.NewStatus == ProductStatus.Active);

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

            // «Новый товар» подписчикам — только при ПЕРВОЙ публикации (никогда ранее не был Active).
            // Повторная публикация ранее опубликованного (просто раскрытие из Hidden) — без рассылки.
            // Фоновая рассылка в собственном scope — не блокирует ответ, свой DbContext.
            if (!everPublished && oldStatus == ProductStatus.Hidden && product.ProductStatus == ProductStatus.Active)
                _ = ShopFollowerNotifier.NotifyNewProductInScopeAsync(
                    _scopeFactory, product.OwnerId, product.Name, product.Slug);

            // Перегенерировать og:image при повторной публикации (Hidden → Active)
            if (oldStatus == ProductStatus.Hidden && product.ProductStatus == ProductStatus.Active)
                _ = ProductOgGenerator.GenerateInScopeAsync(_scopeFactory, product.Id);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record ModerationQueueItemDTO(
    int     ProductId,
    string  ProductName,
    string  ProductSlug,
    string  ShopName,
    Guid    ShopId,
    string? PreviewKey,
    DateTime SentAt);

public record GetProductsOnModerationQuery(string Status = "OnModerating")
    : IRequest<BaseResponse<List<ModerationQueueItemDTO>>>;

public class GetProductsOnModerationHandler
    : IRequestHandler<GetProductsOnModerationQuery, BaseResponse<List<ModerationQueueItemDTO>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IStorageService    _storageService;

    public GetProductsOnModerationHandler(IProductRepository productRepository, IStorageService storageService)
    {
        _productRepository = productRepository;
        _storageService    = storageService;
    }

    public async Task<BaseResponse<List<ModerationQueueItemDTO>>> Handle(
        GetProductsOnModerationQuery request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<ProductStatus>(request.Status, ignoreCase: true, out var status))
                return BaseResponse<List<ModerationQueueItemDTO>>.Fail($"Неизвестный статус: {request.Status}");

            var items = await _productRepository.GetProductsByStatusAsync(status, ct);

            var signed = items.Select(x =>
                x.PreviewKey != null
                    ? x with { PreviewKey = _storageService.GeneratePresignedUrl(x.PreviewKey, 60) }
                    : x
            ).ToList();

            return BaseResponse<List<ModerationQueueItemDTO>>.Success(signed);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ModerationQueueItemDTO>>.Fail(ex.Message);
        }
    }
}

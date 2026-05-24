using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Storage;

public record ContentDetachInfoQuery(int ProductId, Guid StorageObjectId, Guid ShopId)
    : IRequest<BaseResponse<ContentDetachInfoResult>>;

public record ContentDetachInfoResult(bool CanDetachFree, int AccessCount);

public class ContentDetachInfoHandler
    : IRequestHandler<ContentDetachInfoQuery, BaseResponse<ContentDetachInfoResult>>
{
    private readonly IProductRepository       _productRepository;
    private readonly IContentFileRepository   _contentFileRepository;

    public ContentDetachInfoHandler(
        IProductRepository productRepository,
        IContentFileRepository contentFileRepository)
    {
        _productRepository     = productRepository;
        _contentFileRepository = contentFileRepository;
    }

    public async Task<BaseResponse<ContentDetachInfoResult>> Handle(
        ContentDetachInfoQuery request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return BaseResponse<ContentDetachInfoResult>.Fail("Product not found");
        if (product.OwnerId != request.ShopId)
            return BaseResponse<ContentDetachInfoResult>.Fail("Access denied");

        // Найти ContentFile для этого StorageObject в этом Product
        var contentFiles = await _contentFileRepository.GetByProductIdAsync(request.ProductId);
        var cf = contentFiles.FirstOrDefault(c => c.StorageObjectId == request.StorageObjectId);
        if (cf == null)
            return BaseResponse<ContentDetachInfoResult>.Fail("Content file not found");

        var accessCount = await _contentFileRepository.GetAccessCountAsync(cf.Id);

        return BaseResponse<ContentDetachInfoResult>.Success(
            new ContentDetachInfoResult(
                CanDetachFree: accessCount == 0,
                AccessCount:   accessCount));
    }
}

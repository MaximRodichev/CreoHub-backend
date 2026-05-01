using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetProductOwnershipQuery(Guid UserId) : IRequest<BaseResponse<ProductOwnershipDTO>>;

public class GetProductOwnershipHandler
    : IRequestHandler<GetProductOwnershipQuery, BaseResponse<ProductOwnershipDTO>>
{
    private readonly IContentAccessRepository  _accessRepo;
    private readonly IContentFileRepository    _contentFileRepo;
    private readonly IProductRepository        _productRepo;

    public GetProductOwnershipHandler(
        IContentAccessRepository  accessRepo,
        IContentFileRepository    contentFileRepo,
        IProductRepository        productRepo)
    {
        _accessRepo      = accessRepo;
        _contentFileRepo = contentFileRepo;
        _productRepo     = productRepo;
    }

    public async Task<BaseResponse<ProductOwnershipDTO>> Handle(
        GetProductOwnershipQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = new ProductOwnershipDTO();

            // 1. Все доступы пользователя с загруженным ContentFile
            var accesses = await _accessRepo.GetUserFilesAsync(request.UserId);
            if (accesses.Count == 0)
                return BaseResponse<ProductOwnershipDTO>.Success(result);

            var ownedFileIds = accesses.Select(a => a.ContentFileId).ToHashSet();

            // 2. Группируем купленные файлы по продукту
            var ownedCountByProduct = accesses
                .GroupBy(a => a.ContentFile.ProductId)
                .ToDictionary(g => g.Key, g => g.Count());

            var ownedProductIds = ownedCountByProduct.Keys.ToList();

            // 3. Получаем все файлы этих продуктов (для подсчёта totalCount)
            var allFilesForOwned = await _contentFileRepo.GetByProductIdsAsync(ownedProductIds);
            var totalCountByProduct = allFilesForOwned
                .GroupBy(f => f.ProductId)
                .ToDictionary(g => g.Key, g => g.Count());

            // 4. Классифицируем обычные продукты
            foreach (var (productId, ownedCount) in ownedCountByProduct)
            {
                var total = totalCountByProduct.GetValueOrDefault(productId, 0);
                if (total == 0) continue;

                if (ownedCount >= total) result.FullyOwned.Add(productId);
                else                     result.PartiallyOwned.Add(productId);
            }

            // 5. Классифицируем бандлы, у которых есть хотя бы один дочерний продукт в ownedProductIds
            var bundles = await _productRepo.GetBundlesByChildProductIdsAsync(ownedProductIds);

            foreach (var bundle in bundles)
            {
                var childIds = bundle.BundleItems.Select(b => b.ProductId).ToList();
                if (childIds.Count == 0) continue;

                // Для каждого дочернего продукта проверяем, полностью ли он куплен
                // Продукт полностью куплен, если он есть в result.FullyOwned
                bool allChildrenFull = childIds.All(cid => result.FullyOwned.Contains(cid));
                bool anyChildOwned   = childIds.Any(cid =>
                    result.FullyOwned.Contains(cid) || result.PartiallyOwned.Contains(cid));

                if (allChildrenFull)       result.FullyOwned.Add(bundle.Id);
                else if (anyChildOwned)    result.PartiallyOwned.Add(bundle.Id);
            }

            return BaseResponse<ProductOwnershipDTO>.Success(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<ProductOwnershipDTO>.Fail(ex.Message);
        }
    }
}

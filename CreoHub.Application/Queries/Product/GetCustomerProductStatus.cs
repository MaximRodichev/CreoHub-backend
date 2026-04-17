using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Product;

public record GetCustomerProductStatusQuery(Guid UserId, int productId) : IRequest<BaseResponse<UserProductStatusDTO>>;

public class GetCustomerProductStatusHandler : IRequestHandler<GetCustomerProductStatusQuery, BaseResponse<UserProductStatusDTO>>
{
    private readonly IContentAccessRepository _contentAccessRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public GetCustomerProductStatusHandler(IContentAccessRepository contentAccessRepository, IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _contentAccessRepository = contentAccessRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<UserProductStatusDTO>> Handle(GetCustomerProductStatusQuery request, CancellationToken cancellationToken)
    {
        var contentFiles = await _productRepository.GetContentFilesOfProduct(request.productId);

        if (contentFiles.Count == 0)
            return BaseResponse<UserProductStatusDTO>.Success(new UserProductStatusDTO
            {
                Status = UserProductStatus.NotPurchased,
                ContentFilesIds = new List<Guid>()
            });

        var userAccesses = await _contentAccessRepository.GetByUserIdAsync(request.UserId);
        var productFileIds = contentFiles.Select(f => f.Id).ToHashSet();
        var ownedFileIds   = userAccesses
            .Select(a => a.ContentFileId)
            .Where(id => productFileIds.Contains(id))
            .ToList();

        var status = ownedFileIds.Count == 0
            ? UserProductStatus.NotPurchased
            : ownedFileIds.Count == contentFiles.Count
                ? UserProductStatus.Purchased
                : UserProductStatus.PartiallyPurchased;

        return BaseResponse<UserProductStatusDTO>.Success(new UserProductStatusDTO
        {
            Status          = status,
            ContentFilesIds = ownedFileIds
        });
    }
}
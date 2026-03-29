using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record CreateProductBundleCommand(Guid userId, CreateProductBundleDTO dto) : IRequest<BaseResponse<bool>>;

public class CreateProductBundleHandler : IRequestHandler<CreateProductBundleCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductBundleRepository _productBundleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShopRepository _shopRepository;

    public CreateProductBundleHandler(IProductRepository productRepository,
        IProductBundleRepository productBundleRepository,
        IUnitOfWork unitOfWork,
        IShopRepository shopRepository)
    {
        _productRepository = productRepository;
        _productBundleRepository = productBundleRepository;
        _shopRepository = shopRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BaseResponse<bool>> Handle(CreateProductBundleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var responseProducts = await _productRepository.GetProductsByIds(request.dto.ProductIds.ToList());
            var shopOwner = await _shopRepository.GetByOwnerIdAsync(request.userId);

            var productBundle = new Product(request.dto.Name, request.dto.Description, shopOwner.Id, null);

            productBundle.AddPrice(request.dto.Price);
            productBundle.AddBundleItems(responseProducts);

            await _productRepository.AddAsync(productBundle);
            await _productBundleRepository.AddRangeAsync(productBundle.BundleItems.ToList());
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
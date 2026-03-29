using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record CreateProductBundleCommand(Guid userId, CreateProductBundleDTO dto) : IRequest<BaseResponse<bool>>;

public class CreateProductBundleHandler : IRequestHandler<CreateProductBundleCommand, BaseResponse<bool>>
{
    IProductRepository _productRepository;
    IProductBundleRepository _productBundleRepository;
    IPriceRepository _priceRepository;
    ITagRepository _tagRepository;
    IUnitOfWork _unitOfWork;
    IShopRepository _shopRepository;

    public CreateProductBundleHandler(IProductRepository productRepository,
        IProductBundleRepository productBundleRepository,
        IUnitOfWork unitOfWork,
        IPriceRepository priceRepository,
        ITagRepository tagRepository,
        IShopRepository shopRepository)
    {
        _productRepository = productRepository;
        _productBundleRepository = productBundleRepository;
        _priceRepository = priceRepository;
        _tagRepository = tagRepository;
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
            
            var price = new Price(request.dto.Price, productBundle);
            
            await _productRepository.AddAsync(productBundle);
            productBundle.AddBundleItems(responseProducts);
            await _productBundleRepository.AddRangeAsync(productBundle.BundleItems.ToList());
            await _priceRepository.AddAsync(price);
            Console.WriteLine(productBundle);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
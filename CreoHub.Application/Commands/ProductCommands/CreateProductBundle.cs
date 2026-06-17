using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record CreateProductBundleCommand(Guid userId, CreateProductBundleDTO dto) : IRequest<BaseResponse<int>>;

public class CreateProductBundleHandler : IRequestHandler<CreateProductBundleCommand, BaseResponse<int>>
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

    public async Task<BaseResponse<int>> Handle(CreateProductBundleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var responseProducts = await _productRepository.GetProductsByIds(request.dto.ProductIds.ToList());
            var shopOwner = await _shopRepository.GetByOwnerIdAsync(request.userId);

            var productBundle = new Product(request.dto.Name, request.dto.Description, shopOwner.Id, null);

            if (!string.IsNullOrWhiteSpace(request.dto.Slug))
                productBundle.UpdateSlug(request.dto.Slug);

            productBundle.AddPrice(request.dto.Price);
            productBundle.AddBundleItems(responseProducts);

            await _productRepository.AddAsync(productBundle);
            await _productBundleRepository.AddRangeAsync(productBundle.BundleItems.ToList());
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<int>.Success(productBundle.Id);
        }
        catch (Exception ex) when (ExceptionChainContains(ex, "IX_Products_Name"))
        {
            return BaseResponse<int>.Fail("Товар с таким названием уже существует. Выберите другое название.");
        }
        catch (Exception ex) when (ExceptionChainContains(ex, "IX_Products_Slug"))
        {
            return BaseResponse<int>.Fail("Товар с таким URL (slug) уже существует. Укажите другой URL.");
        }
        catch (Exception ex)
        {
            return BaseResponse<int>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Walks the full InnerException chain checking every message —
    /// needed because the UoW may wrap DbUpdateException in a custom exception.
    /// </summary>
    private static bool ExceptionChainContains(Exception? ex, string text)
    {
        while (ex is not null)
        {
            if (ex.Message.Contains(text)) return true;
            ex = ex.InnerException;
        }
        return false;
    }
}
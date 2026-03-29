using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;
public record CreateProductCommand(Guid userId, CreateProductDTO dto) : IRequest<BaseResponse<bool>>;

public class CreateProductHandler :  IRequestHandler<CreateProductCommand, BaseResponse<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IShopRepository _shopRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    
    public CreateProductHandler(IProductRepository productRepository, IAccountRepository accountRepository, IShopRepository shopRepository, ITagRepository tagRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _accountRepository = accountRepository;
        _shopRepository = shopRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Shop shop = await _shopRepository.GetByOwnerIdAsync(request.userId);
            var tags = await _tagRepository.GetByNamesAsync(request.dto.Tags);

            Product product = new Product(
                request.dto.Name,
                request.dto.Description,
                shop.Id,
                tags);

            product.AddPrice(request.dto.Price);

            await _productRepository.AddAsync(product);
            
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
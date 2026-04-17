using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.CartCommands;

public record ToggleCartItemQuery(Guid UserId, int ProductId) : IRequest<BaseResponse<bool>>;

public class ToggleCartItemHandler : IRequestHandler<ToggleCartItemQuery, BaseResponse<bool>>
{
    
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleCartItemHandler(ICartRepository cartRepository, IUnitOfWork unitOfWork,  IProductRepository productRepository)
    {
        _unitOfWork = unitOfWork;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }
    
    public async Task<BaseResponse<bool>> Handle(ToggleCartItemQuery request, CancellationToken cancellationToken)
    {
        try
        {
             CartItem? cartItem = await _cartRepository.GetCartItemByUserAndProduct(request.UserId, request.ProductId);
             if (cartItem == null)
             {
                 //TODO: ALL FILES
                 var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
                 var contentFiles = await _productRepository.GetContentFilesOfProduct(request.ProductId);
                 cartItem = CartItem.Create(cart.Id, request.ProductId, contentFiles.Select(x=>x.Id).ToArray());
                 await _cartRepository.AddCartItem(cartItem);
             }
             else
             {
                 await _cartRepository.RemoveCartItem(cartItem);
             }
             await _unitOfWork.SaveChangesAsync(cancellationToken);
             return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.CartCommands;

public record ToggleCartItemQuery(
    Guid    UserId,
    int     ProductId,
    string? SessionId = null
) : IRequest<BaseResponse<bool>>;

public class ToggleCartItemHandler : IRequestHandler<ToggleCartItemQuery, BaseResponse<bool>>
{
    private readonly ICartRepository    _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork        _unitOfWork;
    private readonly IEventTracker      _events;

    public ToggleCartItemHandler(
        ICartRepository    cartRepository,
        IUnitOfWork        unitOfWork,
        IProductRepository productRepository,
        IEventTracker      events)
    {
        _unitOfWork        = unitOfWork;
        _cartRepository    = cartRepository;
        _productRepository = productRepository;
        _events            = events;
    }

    public async Task<BaseResponse<bool>> Handle(ToggleCartItemQuery request, CancellationToken cancellationToken)
    {
        try
        {
             CartItem? cartItem = await _cartRepository.GetCartItemByUserAndProduct(request.UserId, request.ProductId);
             bool adding;
             if (cartItem == null)
             {
                 //TODO: ALL FILES
                 var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
                 var contentFiles = await _productRepository.GetContentFilesOfProduct(request.ProductId);
                 cartItem = CartItem.Create(cart.Id, request.ProductId, contentFiles.Select(x=>x.Id).ToArray());
                 await _cartRepository.AddCartItem(cartItem);
                 adding = true;
             }
             else
             {
                 await _cartRepository.RemoveCartItem(cartItem);
                 adding = false;
             }
             await _unitOfWork.SaveChangesAsync(cancellationToken);

             _events.Track(
                 adding ? EventTypes.CartAdd : EventTypes.CartRemove,
                 productId: request.ProductId,
                 userId:    request.UserId,
                 sessionId: request.SessionId);

             return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
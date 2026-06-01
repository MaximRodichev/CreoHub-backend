using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.CartCommands;

public record ToggleCartItemQuery(
    Guid         UserId,
    int          ProductId,
    List<Guid>?  SelectedFileIds = null,
    string?      SessionId = null
) : IRequest<BaseResponse<Guid?>>;

public class ToggleCartItemHandler : IRequestHandler<ToggleCartItemQuery, BaseResponse<Guid?>>
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

    public async Task<BaseResponse<Guid?>> Handle(ToggleCartItemQuery request, CancellationToken cancellationToken)
    {
        try
        {
             CartItem? cartItem = await _cartRepository.GetCartItemByUserAndProduct(request.UserId, request.ProductId);
             bool adding;
             Guid? cartItemId = null;
             if (cartItem == null)
             {
                 var cart         = await _cartRepository.GetByUserIdAsync(request.UserId);
                 var contentFiles = await _productRepository.GetContentFilesOfProduct(request.ProductId);

                 // Если фронтенд передал конкретный список файлов — используем его,
                 // иначе добавляем все доступные файлы продукта
                 var fileIds = request.SelectedFileIds is { Count: > 0 }
                     ? request.SelectedFileIds
                     : contentFiles.Select(x => x.Id).ToList();

                 cartItem = CartItem.Create(cart.Id, request.ProductId, fileIds);
                 await _cartRepository.AddCartItem(cartItem);
                 cartItemId = cartItem.Id;
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

             return BaseResponse<Guid?>.Success(cartItemId);
        }
        catch (Exception ex)
        {
            return BaseResponse<Guid?>.Fail(ex.Message);
        }
    }
}
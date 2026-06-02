using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;
using System.Linq;

namespace CreoHub.Application.Commands.CartCommands;

public record UpdateCartItemFilesCommand(Guid UserId, Guid CartItemId, List<Guid> FileIds)
    : IRequest<BaseResponse<bool>>;

public class UpdateCartItemFilesHandler
    : IRequestHandler<UpdateCartItemFilesCommand, BaseResponse<bool>>
{
    private readonly ICartRepository        _cartRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IUnitOfWork            _unitOfWork;

    public UpdateCartItemFilesHandler(
        ICartRepository        cartRepository,
        IContentFileRepository contentFileRepository,
        IUnitOfWork            unitOfWork)
    {
        _cartRepository        = cartRepository;
        _contentFileRepository = contentFileRepository;
        _unitOfWork            = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        UpdateCartItemFilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cartItem = await _cartRepository.GetCartItemWithFilesAsync(request.CartItemId);
            if (cartItem is null)
                return BaseResponse<bool>.Fail("Cart item not found.");

            // Проверяем что CartItem принадлежит именно этому пользователю
            var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
            if (cart is null || cartItem.CartId != cart.Id)
                return BaseResponse<bool>.Fail("Access denied.");

            // Проверяем что все fileIds принадлежат именно этому товару
            if (request.FileIds.Count > 0)
            {
                var validIds = await _contentFileRepository
                    .GetIdsByProductIdAsync(cartItem.ProductId, cancellationToken);
                var invalid = request.FileIds.FirstOrDefault(id => !validIds.Contains(id));
                if (invalid != Guid.Empty)
                    return BaseResponse<bool>.Fail("Один или несколько файлов не принадлежат данному товару.");
            }

            // Доменный метод: очищает старые CartItemFile и добавляет новые
            cartItem.UpdateFiles(request.FileIds);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

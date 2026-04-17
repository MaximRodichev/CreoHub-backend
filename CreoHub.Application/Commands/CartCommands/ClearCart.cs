using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.CartCommands;

public record ClearCartCommand(Guid UserId) : IRequest<BaseResponse<bool>>;

public class ClearCartHandler : IRequestHandler<ClearCartCommand, BaseResponse<bool>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClearCartHandler(ICartRepository cartRepository, IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        ClearCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _cartRepository.ClearCartAsync(request.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

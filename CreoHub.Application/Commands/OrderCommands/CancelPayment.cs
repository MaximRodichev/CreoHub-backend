using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.OrderCommands;

/// <summary>
/// Отмена заказа при получении Expired или Failed от OxaPay.
/// </summary>
public record CancelPaymentCommand(string TrackId, bool IsExpired)
    : IRequest<BaseResponse<bool>>;

public class CancelPaymentHandler : IRequestHandler<CancelPaymentCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IOrderRepository _orderRepository;

    public CancelPaymentHandler(
        IUnitOfWork unitOfWork,
        IUserTransactionRepository transactionRepository,
        IOrderRepository orderRepository)
    {
        _unitOfWork = unitOfWork;
        _transactionRepository = transactionRepository;
        _orderRepository = orderRepository;
    }

    public async Task<BaseResponse<bool>> Handle(
        CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _transactionRepository.GetByTrackIdAsync(request.TrackId)
                ?? throw new InvalidOperationException(
                    $"Transaction with trackId '{request.TrackId}' not found.");

            // Транзакции UpBalance не имеют заказа — обрабатываем отдельно
            if (transaction.TransactionType == TransactionType.UpBalance)
            {
                if (request.IsExpired) transaction.Expire();
                else transaction.Fail();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return BaseResponse<bool>.Success(true);
            }

            // Purchase — нужно отменить и заказ
            var order = await _orderRepository.GetByTransactionIdWithItemsAsync(transaction.Id)
                ?? throw new InvalidOperationException(
                    $"Order for transaction '{request.TrackId}' not found.");

            if (request.IsExpired) transaction.Expire();
            else transaction.Fail();

            order.Cancel();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception)
        {
            return BaseResponse<bool>.Fail("Не удалось отменить платёж.");
        }
    }
}

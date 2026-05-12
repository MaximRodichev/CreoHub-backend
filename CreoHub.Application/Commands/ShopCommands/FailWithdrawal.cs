using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

/// <summary>
/// Вебхук OxaPay "Failed" — вывод не прошёл.
/// Возвращаем Pending → Available.
/// </summary>
public record FailWithdrawalCommand(string TrackId) : IRequest<BaseResponse<bool>>;

public class FailWithdrawalHandler : IRequestHandler<FailWithdrawalCommand, BaseResponse<bool>>
{
    private readonly IShopTransactionRepository _shopTransactionRepository;
    private readonly IShopBalanceRepository     _shopBalanceRepository;
    private readonly IUnitOfWork                _unitOfWork;

    public FailWithdrawalHandler(
        IShopTransactionRepository shopTransactionRepository,
        IShopBalanceRepository     shopBalanceRepository,
        IUnitOfWork                unitOfWork)
    {
        _shopTransactionRepository = shopTransactionRepository;
        _shopBalanceRepository     = shopBalanceRepository;
        _unitOfWork                = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        FailWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var tx = await _shopTransactionRepository.GetByTrackIdAsync(request.TrackId);
        if (tx is null)
            return BaseResponse<bool>.Fail($"ShopTransaction not found: {request.TrackId}");

        if (tx.TransactionStatus != Domain.Types.TransactionStatus.Pending)
            return BaseResponse<bool>.Success(true); // Идемпотентность

        var balance = await _shopBalanceRepository.GetByShopIdAsync(tx.ShopId);
        if (balance is null)
            return BaseResponse<bool>.Fail($"ShopBalance not found for shop: {tx.ShopId}");

        tx.Fail();
        balance.CancelWithdraw(); // Pending → Available (возврат)

        _shopTransactionRepository.Update(tx);
        _shopBalanceRepository.Update(balance);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true);
    }
}

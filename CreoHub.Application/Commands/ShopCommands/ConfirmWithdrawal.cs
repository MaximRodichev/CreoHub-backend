using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

/// <summary>
/// Вебхук OxaPay "Confirmed" — транзакция прошла в блокчейне.
/// Обнуляем Pending (деньги ушли насовсем).
/// </summary>
public record ConfirmWithdrawalCommand(string TrackId, string TxHash) : IRequest<BaseResponse<bool>>;

public class ConfirmWithdrawalHandler : IRequestHandler<ConfirmWithdrawalCommand, BaseResponse<bool>>
{
    private readonly IShopTransactionRepository _shopTransactionRepository;
    private readonly IShopBalanceRepository     _shopBalanceRepository;
    private readonly IUnitOfWork                _unitOfWork;

    public ConfirmWithdrawalHandler(
        IShopTransactionRepository shopTransactionRepository,
        IShopBalanceRepository     shopBalanceRepository,
        IUnitOfWork                unitOfWork)
    {
        _shopTransactionRepository = shopTransactionRepository;
        _shopBalanceRepository     = shopBalanceRepository;
        _unitOfWork                = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        ConfirmWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var tx = await _shopTransactionRepository.GetByTrackIdAsync(request.TrackId);
        if (tx is null)
            return BaseResponse<bool>.Fail($"ShopTransaction not found: {request.TrackId}");

        if (tx.TransactionStatus != Domain.Types.TransactionStatus.Pending)
            return BaseResponse<bool>.Success(true); // Идемпотентность — уже обработано

        var balance = await _shopBalanceRepository.GetByShopIdAsync(tx.ShopId);
        if (balance is null)
            return BaseResponse<bool>.Fail($"ShopBalance not found for shop: {tx.ShopId}");

        // TxHash есть только для внешних on-chain выводов
        if (!string.IsNullOrWhiteSpace(request.TxHash))
            tx.Success(senderAddress: "oxapay", txHash: request.TxHash);
        else
            tx.SuccessInternal();

        balance.CompleteWithdraw(); // Pending → 0

        _shopTransactionRepository.Update(tx);
        _shopBalanceRepository.Update(balance);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse<bool>.Success(true);
    }
}

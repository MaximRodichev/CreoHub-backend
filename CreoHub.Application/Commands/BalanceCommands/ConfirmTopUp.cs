using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Application.Commands.BalanceCommands;

/// <summary>
/// Зачисление средств на баланс после успешной оплаты OxaPay (UpBalance транзакция).
/// </summary>
public record ConfirmTopUpCommand(string TrackId, string TxHash, string SenderAddress)
    : IRequest<BaseResponse<bool>>;

public class ConfirmTopUpHandler : IRequestHandler<ConfirmTopUpCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IUserBalanceRepository _balanceRepository;

    public ConfirmTopUpHandler(
        IUnitOfWork unitOfWork,
        IUserTransactionRepository transactionRepository,
        IUserBalanceRepository balanceRepository)
    {
        _unitOfWork = unitOfWork;
        _transactionRepository = transactionRepository;
        _balanceRepository = balanceRepository;
    }

    public async Task<BaseResponse<bool>> Handle(
        ConfirmTopUpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _transactionRepository.GetByTrackIdAsync(request.TrackId)
                ?? throw new InvalidOperationException(
                    $"Transaction with trackId '{request.TrackId}' not found.");

            // Idempotency: webhook может прийти дважды — уже обработанный trackId игнорируем
            if (transaction.TransactionStatus == TransactionStatus.Completed)
                return BaseResponse<bool>.Success(true);

            transaction.Success(request.SenderAddress, request.TxHash);

            // Загрузить или создать баланс пользователя
            var balance = await _balanceRepository.GetByUserIdAsync(transaction.UserId);
            if (balance is null)
            {
                var newBalance = new Domain.Entities.UserBalance(transaction.UserId);
                newBalance.AddFunds(transaction.FullAmount);
                await _balanceRepository.AddAsync(newBalance);
            }
            else
            {
                balance.AddFunds(transaction.FullAmount);
                _balanceRepository.Update(balance);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Гонка webhook'ов: другой запрос уже зачислил этот trackId — идемпотентный успех.
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Application.Commands.BalanceCommands;

/// <summary>
/// Зачисление средств на баланс после успешной оплаты OxaPay (UpBalance транзакция).
/// </summary>
/// <summary>
/// Зачисление пополнения. ReceivedAmount — фактически полученное (value из вебхука,
/// после сетевой комиссии). Зачисляем именно его, а не запрошенное (FullAmount):
/// при переплате юзер получает что заплатил, при недоплате — не теряем деньги.
/// </summary>
public record ConfirmTopUpCommand(string TrackId, string TxHash, string SenderAddress, decimal ReceivedAmount)
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

            // Зачисляем фактически полученное (value), а не запрошенное.
            // Округление до цента вниз — чтобы не зачислить больше, чем реально пришло.
            // Fallback на FullAmount, если value по какой-то причине не передали (>0 защита).
            var credited = request.ReceivedAmount > 0
                ? Math.Floor(request.ReceivedAmount * 100m) / 100m
                : transaction.FullAmount;

            transaction.Success(request.SenderAddress, request.TxHash, paidAmount: credited);

            // Загрузить или создать баланс пользователя
            var balance = await _balanceRepository.GetByUserIdAsync(transaction.UserId);
            if (balance is null)
            {
                var newBalance = new Domain.Entities.UserBalance(transaction.UserId);
                newBalance.AddFunds(credited);
                await _balanceRepository.AddAsync(newBalance);
            }
            else
            {
                balance.AddFunds(credited);
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
        catch (Exception)
        {
            return BaseResponse<bool>.Fail("Не удалось зачислить пополнение.");
        }
    }
}

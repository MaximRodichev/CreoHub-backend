using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

public record WithdrawShopBalanceCommand(Guid ShopId, decimal Amount, string Address, string Network)
    : IRequest<BaseResponse<bool>>;

public class WithdrawShopBalanceHandler : IRequestHandler<WithdrawShopBalanceCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShopBalanceRepository _shopBalanceRepository;
    private readonly IShopTransactionRepository _shopTransactionRepository;
    private readonly IPaymentGatewayService _paymentService;

    public WithdrawShopBalanceHandler(
        IUnitOfWork unitOfWork,
        IShopBalanceRepository shopBalanceRepository,
        IShopTransactionRepository shopTransactionRepository,
        IPaymentGatewayService paymentService)
    {
        _unitOfWork = unitOfWork;
        _shopBalanceRepository = shopBalanceRepository;
        _shopTransactionRepository = shopTransactionRepository;
        _paymentService = paymentService;
    }

    public async Task<BaseResponse<bool>> Handle(
        WithdrawShopBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
                return BaseResponse<bool>.Fail("Сумма вывода должна быть больше нуля.");

            if (string.IsNullOrWhiteSpace(request.Address))
                return BaseResponse<bool>.Fail("Укажите адрес кошелька.");

            if (string.IsNullOrWhiteSpace(request.Network))
                return BaseResponse<bool>.Fail("Укажите сеть.");

            var balance = await _shopBalanceRepository.GetByShopIdAsync(request.ShopId);
            if (balance is null)
                return BaseResponse<bool>.Fail("Баланс магазина не найден.");

            if (balance.AvailableAmount < request.Amount)
                return BaseResponse<bool>.Fail(
                    $"Недостаточно средств. Доступно: ${balance.AvailableAmount:F2}");

            if (balance.PendingAmount > 0)
                return BaseResponse<bool>.Fail(
                    "Дождитесь завершения предыдущего вывода перед новым запросом.");

            var trackId = $"withdraw-{request.ShopId}-{Guid.NewGuid()}";

            // Резервируем сумму (Available → Pending)
            var shopTx = ShopTransaction.CreateWithdrawal(request.Amount, request.ShopId, trackId);
            balance.WithdrawFunds(request.Amount);

            await _shopTransactionRepository.AddAsync(shopTx);
            _shopBalanceRepository.Update(balance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Отправляем через OxaPay
            try
            {
                await _paymentService.CreatePayoutAsync(request.Amount, request.Address, request.Network);

                // OxaPay принял запрос — считаем вывод завершённым
                shopTx.SuccessInternal();
                balance.CompleteWithdraw();
            }
            catch (Exception payEx)
            {
                // OxaPay вернул ошибку — откатываем резерв
                shopTx.Fail();
                balance.CancelWithdraw();

                _shopBalanceRepository.Update(balance);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return BaseResponse<bool>.Fail($"Ошибка платёжного шлюза: {payEx.Message}");
            }

            _shopBalanceRepository.Update(balance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

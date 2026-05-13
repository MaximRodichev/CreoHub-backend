using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

public record TransferShopToUserBalanceCommand(Guid ShopId, Guid UserId, decimal Amount)
    : IRequest<BaseResponse<bool>>;

public class TransferShopToUserBalanceHandler
    : IRequestHandler<TransferShopToUserBalanceCommand, BaseResponse<bool>>
{
    private readonly IShopBalanceRepository     _shopBalanceRepository;
    private readonly IUserBalanceRepository     _userBalanceRepository;
    private readonly IShopTransactionRepository _shopTransactionRepository;
    private readonly IUnitOfWork                _unitOfWork;

    public TransferShopToUserBalanceHandler(
        IShopBalanceRepository     shopBalanceRepository,
        IUserBalanceRepository     userBalanceRepository,
        IShopTransactionRepository shopTransactionRepository,
        IUnitOfWork                unitOfWork)
    {
        _shopBalanceRepository     = shopBalanceRepository;
        _userBalanceRepository     = userBalanceRepository;
        _shopTransactionRepository = shopTransactionRepository;
        _unitOfWork                = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(
        TransferShopToUserBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
                return BaseResponse<bool>.Fail("Сумма перевода должна быть больше нуля.");

            // ── Shop balance ─────────────────────────────────────────────────
            var shopBalance = await _shopBalanceRepository.GetByShopIdAsync(request.ShopId);
            if (shopBalance is null)
                return BaseResponse<bool>.Fail("Баланс магазина не найден.");

            if (shopBalance.AvailableAmount < request.Amount)
                return BaseResponse<bool>.Fail(
                    $"Недостаточно средств. Доступно: ${shopBalance.AvailableAmount:F2}");

            // ── User balance ─────────────────────────────────────────────────
            var userBalance = await _userBalanceRepository.GetByUserIdAsync(request.UserId);
            if (userBalance is null)
                return BaseResponse<bool>.Fail("Баланс пользователя не найден.");

            // ── Execute transfer ─────────────────────────────────────────────
            shopBalance.Spend(request.Amount);
            userBalance.AddFunds(request.Amount);

            var trackId = $"transfer-{request.ShopId}-{Guid.NewGuid()}";
            var tx = ShopTransaction.CreateTransfer(request.Amount, request.ShopId, trackId);

            await _shopTransactionRepository.AddAsync(tx);
            _shopBalanceRepository.Update(shopBalance);
            _userBalanceRepository.Update(userBalance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

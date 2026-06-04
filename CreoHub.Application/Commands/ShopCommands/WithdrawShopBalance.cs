using System.Text.RegularExpressions;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Application.Commands.ShopCommands;

public record WithdrawShopBalanceCommand(Guid ShopId, decimal Amount, string Address, string Network)
    : IRequest<BaseResponse<bool>>;

public class WithdrawShopBalanceHandler : IRequestHandler<WithdrawShopBalanceCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork                    _unitOfWork;
    private readonly IShopBalanceRepository         _shopBalanceRepository;
    private readonly IShopTransactionRepository     _shopTransactionRepository;
    private readonly IPaymentGatewayService         _paymentService;

    // ── Минимальные суммы вывода по сети (сетевые комиссии) ──────────────────
    private static readonly Dictionary<string, decimal> MinWithdrawal = new()
    {
        ["TRC20"]   = 5m,
        ["BEP20"]   = 5m,
        ["ERC20"]   = 20m,   // ETH gas дорогой
        ["Bitcoin"] = 10m,
        ["Polygon"] = 5m,
        ["Solana"]  = 5m,
    };

    // ── Regexp валидация адреса по сети ──────────────────────────────────────
    private static readonly Dictionary<string, Regex> AddressRegex = new()
    {
        ["TRC20"]   = new Regex(@"^T[1-9A-HJ-NP-Za-km-z]{33}$",          RegexOptions.Compiled),
        ["BEP20"]   = new Regex(@"^0x[0-9a-fA-F]{40}$",                   RegexOptions.Compiled),
        ["ERC20"]   = new Regex(@"^0x[0-9a-fA-F]{40}$",                   RegexOptions.Compiled),
        ["Bitcoin"] = new Regex(@"^(1|3)[1-9A-HJ-NP-Za-km-z]{25,34}$|^bc1[ac-hj-np-z02-9]{6,87}$",
                                RegexOptions.Compiled),
        ["Polygon"] = new Regex(@"^0x[0-9a-fA-F]{40}$",                   RegexOptions.Compiled),
        ["Solana"]  = new Regex(@"^[1-9A-HJ-NP-Za-km-z]{32,44}$",        RegexOptions.Compiled),
    };

    // ── Cooldown между выводами ───────────────────────────────────────────────
    private static readonly TimeSpan WithdrawCooldown = TimeSpan.FromHours(1);

    public WithdrawShopBalanceHandler(
        IUnitOfWork                unitOfWork,
        IShopBalanceRepository     shopBalanceRepository,
        IShopTransactionRepository shopTransactionRepository,
        IPaymentGatewayService     paymentService)
    {
        _unitOfWork                = unitOfWork;
        _shopBalanceRepository     = shopBalanceRepository;
        _shopTransactionRepository = shopTransactionRepository;
        _paymentService            = paymentService;
    }

    public async Task<BaseResponse<bool>> Handle(
        WithdrawShopBalanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // ── Базовые проверки ─────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.Address))
                return BaseResponse<bool>.Fail("Укажите адрес кошелька.");

            if (string.IsNullOrWhiteSpace(request.Network))
                return BaseResponse<bool>.Fail("Укажите сеть.");

            // ── Матрица рисков 1+2: формат адреса и соответствие сети ────────
            if (!AddressRegex.TryGetValue(request.Network, out var regex))
                return BaseResponse<bool>.Fail($"Неподдерживаемая сеть: {request.Network}");

            if (!regex.IsMatch(request.Address))
                return BaseResponse<bool>.Fail(
                    $"Адрес не соответствует формату сети {request.Network}. " +
                    GetAddressHint(request.Network));

            // ── Матрица рисков 5: минимальная сумма ──────────────────────────
            if (request.Amount <= 0)
                return BaseResponse<bool>.Fail("Сумма вывода должна быть больше нуля.");

            var minAmount = MinWithdrawal.GetValueOrDefault(request.Network, 5m);
            if (request.Amount < minAmount)
                return BaseResponse<bool>.Fail(
                    $"Минимальная сумма вывода для {request.Network}: ${minAmount:F2}");

            // ── Баланс ───────────────────────────────────────────────────────
            var balance = await _shopBalanceRepository.GetByShopIdAsync(request.ShopId);
            if (balance is null)
                return BaseResponse<bool>.Fail("Баланс магазина не найден.");

            if (balance.AvailableAmount < request.Amount)
                return BaseResponse<bool>.Fail(
                    $"Недостаточно средств. Доступно: ${balance.AvailableAmount:F2}");

            // ── Матрица рисков 5: cooldown + блок при активном Pending ───────
            if (balance.PendingAmount > 0)
                return BaseResponse<bool>.Fail(
                    "Дождитесь завершения предыдущего вывода перед новым запросом.");

            var lastWithdrawal = await _shopTransactionRepository.GetLastWithdrawalAsync(request.ShopId);
            if (lastWithdrawal is not null &&
                lastWithdrawal.TransactionStatus == TransactionStatus.Pending)
                return BaseResponse<bool>.Fail(
                    "Предыдущий вывод ещё обрабатывается.");

            if (lastWithdrawal is not null &&
                lastWithdrawal.TransactionStatus == TransactionStatus.Completed &&
                DateTime.UtcNow - lastWithdrawal.CreatedAt < WithdrawCooldown)
            {
                var wait = WithdrawCooldown - (DateTime.UtcNow - lastWithdrawal.CreatedAt);
                return BaseResponse<bool>.Fail(
                    $"Следующий вывод доступен через {(int)wait.TotalMinutes} мин.");
            }

            // ── Резервируем: Available → Pending ─────────────────────────────
            var trackId = $"withdraw-{request.ShopId}-{Guid.NewGuid()}";
            var shopTx  = ShopTransaction.CreateWithdrawal(request.Amount, request.ShopId, trackId);
            balance.WithdrawFunds(request.Amount);

            await _shopTransactionRepository.AddAsync(shopTx);
            _shopBalanceRepository.Update(balance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── Отправляем через OxaPay ──────────────────────────────────────
            try
            {
                await _paymentService.CreatePayoutAsync(request.Amount, request.Address, request.Network);
                // ✅ OxaPay принял запрос.
                // Pending остаётся до прихода payout-вебхука:
                //   Confirmed → CompleteWithdraw (Pending = 0)
                //   Failed    → CancelWithdraw (Pending → Available)
            }
            catch (Exception)
            {
                // ❌ OxaPay синхронно отклонил (невалидный адрес, лимиты, etc.)
                // — откатываем резерв немедленно. Детали шлюза наружу не отдаём.
                shopTx.Fail();
                balance.CancelWithdraw();

                _shopBalanceRepository.Update(balance);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return BaseResponse<bool>.Fail("Платёжный шлюз отклонил вывод. Проверьте адрес и сеть.");
            }

            _shopBalanceRepository.Update(balance);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Параллельная операция изменила баланс — безопасно просим повторить.
            return BaseResponse<bool>.Fail("Баланс изменился во время операции. Обновите страницу и попробуйте снова.");
        }
        catch (Exception)
        {
            return BaseResponse<bool>.Fail("Не удалось выполнить вывод. Попробуйте позже.");
        }
    }

    private static string GetAddressHint(string network) => network switch
    {
        "TRC20"   => "Tron-адрес начинается с T и содержит 34 символа.",
        "ERC20"   => "Ethereum-адрес начинается с 0x и содержит 42 символа.",
        "BEP20"   => "BSC-адрес начинается с 0x и содержит 42 символа.",
        "Bitcoin" => "Bitcoin-адрес начинается с 1, 3 или bc1.",
        "Polygon" => "Polygon-адрес начинается с 0x и содержит 42 символа.",
        "Solana"  => "Solana-адрес содержит 32–44 символа Base58.",
        _         => ""
    };
}

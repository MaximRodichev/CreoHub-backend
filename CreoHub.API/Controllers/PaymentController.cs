using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CreoHub.Application.Commands.BalanceCommands;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.Commands.ShopCommands;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using CreoHub.Infrastructure.Persistence.DTOs.OxaPay;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IPaymentGatewayService _paymentService;
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;
    private readonly IUserTransactionRepository _transactionRepository;

    protected Guid UserId => Guid.Parse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    public PaymentController(
        ILogger<PaymentController> logger,
        IPaymentGatewayService paymentService,
        IMediator mediator,
        IConfiguration config,
        IUserTransactionRepository transactionRepository)
    {
        _logger = logger;
        _paymentService = paymentService;
        _mediator = mediator;
        _config = config;
        _transactionRepository = transactionRepository;
    }

    /// <summary>
    /// Рассчитать итоговую цену выбранных файлов.
    /// Вызывается перед оплатой — бекенд является источником правды.
    /// </summary>
    [Authorize]
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] List<CheckoutItemDTO> items)
    {
        var response = await _mediator.Send(new CalculateOrderPriceQuery(items, UserId));
        return Ok(response);
    }

    /// <summary>
    /// ЗАБЛОКИРОВАНО — прямая оплата через OxaPay без баланса отключена.
    /// Все покупки проходят через /api/balance/checkout.
    /// </summary>
    [Authorize]
    [HttpPost("checkout")]
    public IActionResult Checkout()
    {
        return StatusCode(410, new { status = 1, errorMessage = "Direct OxaPay checkout is disabled. Use /api/balance/checkout instead." });
    }

    /// <summary>
    /// Получить актуальную ссылку на оплату для заказа в статусе Created.
    /// Используется если пользователь потерял ссылку или она не открылась.
    /// </summary>
    [Authorize]
    [HttpGet("orders/{orderId:guid}/payment-link")]
    public async Task<IActionResult> GetPaymentLink(Guid orderId)
    {
        var response = await _mediator.Send(new GetOrderPaymentLinkQuery(UserId, orderId));
        return Ok(response);
    }

    /// <summary>
    /// Восстановить ссылку на оплату для незавершённого пополнения (Pending UpBalance).
    /// </summary>
    [Authorize]
    [HttpGet("topup/{transactionId:guid}/payment-link")]
    public async Task<IActionResult> GetTopUpPaymentLink(Guid transactionId)
    {
        var response = await _mediator.Send(
            new CreoHub.Application.Queries.Account.GetTopUpPaymentLinkQuery(UserId, transactionId));
        return Ok(response);
    }

    /// <summary>
    /// Вебхук от OxaPay — вызывается при изменении статуса платежа.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromServices] IPaymentWebhookLogRepository webhookLog)
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: false))
            rawBody = await reader.ReadToEndAsync();

        var hmacHeader = Request.Headers["hmac"].ToString();
        var key  = Encoding.UTF8.GetBytes(_config["OxaPay:MerchantApiKey"]!);
        var data = Encoding.UTF8.GetBytes(rawBody);
        using var hmac = new HMACSHA512(key);
        var calculated = Convert.ToHexString(hmac.ComputeHash(data)).ToLower();

        // Constant-time сравнение — защита от timing-атаки на подбор HMAC
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(calculated),
                Encoding.UTF8.GetBytes(hmacHeader.ToLower())))
        {
            _logger.LogWarning("OxaPay webhook: invalid HMAC signature");
            return BadRequest("Invalid signature");
        }

        OxaPayWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OxaPayWebhookPayload>(rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OxaPay webhook: failed to deserialize payload");
            return BadRequest("Invalid payload");
        }

        if (payload == null)
            return BadRequest("Empty payload");

        // Forensic-лог: сохраняем сырой вебхук в БД (независимый коммит).
        // Доступа извне нет — разбор спорных платежей запросом в pgAdmin.
        try
        {
            await webhookLog.SaveAsync(payload.TrackId, payload.Status, rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PaymentWebhookLog save failed for trackId={TrackId}", payload.TrackId);
        }

        _logger.LogInformation("OxaPay webhook: status={Status} trackId={TrackId} amount={Amount} value={Value}",
            payload.Status, payload.TrackId, payload.Amount, payload.Value);

        if (payload.Status == "Paid")
        {
            // Определяем тип транзакции — UpBalance (пополнение) или Purchase (покупка)
            var transaction = await _transactionRepository.GetByTrackIdAsync(payload.TrackId);

            // Защита от пустого массива Transactions (400 от провайдера → 500 без этой проверки)
            var tx = payload.Transactions?.FirstOrDefault();
            var txHash        = tx?.TxHash        ?? string.Empty;
            var senderAddress = tx?.SenderAddress ?? string.Empty;

            if (transaction?.TransactionType == TransactionType.UpBalance)
            {
                var result = await _mediator.Send(new ConfirmTopUpCommand(
                    TrackId:        payload.TrackId,
                    TxHash:         txHash,
                    SenderAddress:  senderAddress,
                    ReceivedAmount: payload.Value));

                if (result.Status != Application.DTO.ResponseStatus.Success)
                    _logger.LogError("ConfirmTopUp failed for trackId={TrackId}: {Error}",
                        payload.TrackId, result.ErrorMessage);
            }
            else
            {
                var result = await _mediator.Send(new CompletePaymentCommand(
                    TrackId:        payload.TrackId,
                    TxHash:         txHash,
                    SenderAddress:  senderAddress,
                    ReceivedAmount: payload.Value));

                if (result.Status != Application.DTO.ResponseStatus.Success)
                    _logger.LogError("CompletePayment failed for trackId={TrackId}: {Error}",
                        payload.TrackId, result.ErrorMessage);
            }
        }
        else if (payload.Status is "Expired" or "Failed")
        {
            var result = await _mediator.Send(new CancelPaymentCommand(
                TrackId:   payload.TrackId,
                IsExpired: payload.Status == "Expired"));

            if (result.Status != Application.DTO.ResponseStatus.Success)
                _logger.LogError("CancelPayment failed for trackId={TrackId}: {Error}",
                    payload.TrackId, result.ErrorMessage);
        }
        else
        {
            _logger.LogInformation("OxaPay webhook: unhandled status={Status}, skipping", payload.Status);
        }

        // OxaPay ожидает HTTP 200 независимо от бизнес-результата
        return Ok("ok");
    }

    /// <summary>
    /// Вебхук от OxaPay — изменение статуса ВЫВОДА средств шопа.
    /// Используется отдельный ключ (PayoutApiKey) и отдельная логика баланса.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("payout-webhook")]
    public async Task<IActionResult> PayoutWebhook()
    {
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: false))
            rawBody = await reader.ReadToEndAsync();

        var hmacHeader = Request.Headers["hmac"].ToString();
        // Payout-вебхук подписывается PayoutApiKey, а не MerchantApiKey
        var key  = Encoding.UTF8.GetBytes(_config["OxaPay:PayoutApiKey"]!);
        var data = Encoding.UTF8.GetBytes(rawBody);
        using var hmac       = new HMACSHA512(key);
        var       calculated = Convert.ToHexString(hmac.ComputeHash(data)).ToLower();

        if (calculated != hmacHeader.ToLower())
        {
            _logger.LogWarning("OxaPay payout-webhook: invalid HMAC");
            return BadRequest("Invalid signature");
        }

        OxaPayWebhookPayload? payload;
        try { payload = JsonSerializer.Deserialize<OxaPayWebhookPayload>(rawBody); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OxaPay payout-webhook: failed to deserialize");
            return BadRequest("Invalid payload");
        }

        if (payload is null) return BadRequest("Empty payload");

        _logger.LogInformation("OxaPay payout-webhook: status={Status} trackId={TrackId}",
            payload.Status, payload.TrackId);

        switch (payload.Status)
        {
            case "Confirming":
                // Промежуточный статус — транзакция в блокчейне, ждём финала
                break;

            case "Confirmed":
            {
                var txHash = payload.Transactions.FirstOrDefault()?.TxHash ?? string.Empty;
                var result = await _mediator.Send(
                    new ConfirmWithdrawalCommand(payload.TrackId, txHash));
                if (result.Status != Application.DTO.ResponseStatus.Success)
                    _logger.LogError("ConfirmWithdrawal failed for trackId={TrackId}: {Error}",
                        payload.TrackId, result.ErrorMessage);
                break;
            }

            case "Failed":
            {
                var result = await _mediator.Send(
                    new FailWithdrawalCommand(payload.TrackId));
                if (result.Status != Application.DTO.ResponseStatus.Success)
                    _logger.LogError("FailWithdrawal failed for trackId={TrackId}: {Error}",
                        payload.TrackId, result.ErrorMessage);
                break;
            }

            default:
                _logger.LogInformation("OxaPay payout-webhook: unhandled status={Status}", payload.Status);
                break;
        }

        return Ok("ok");
    }

}

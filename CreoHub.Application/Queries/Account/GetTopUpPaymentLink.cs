using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.Account;

/// <summary>
/// Восстановить ссылку на оплату для незавершённого пополнения (Pending UpBalance).
/// Используется, если юзер потерял исходную ссылку. Берёт актуальный payment_url
/// из OxaPay по trackId транзакции.
/// </summary>
public record GetTopUpPaymentLinkQuery(Guid UserId, Guid TransactionId)
    : IRequest<BaseResponse<TopUpPaymentLinkDTO>>;

public class TopUpPaymentLinkDTO
{
    public string   PaymentUrl { get; init; } = string.Empty;
    public DateTime ExpiredAt  { get; init; }
}

public class GetTopUpPaymentLinkHandler
    : IRequestHandler<GetTopUpPaymentLinkQuery, BaseResponse<TopUpPaymentLinkDTO>>
{
    private readonly IUserTransactionRepository _transactionRepository;
    private readonly IPaymentGatewayService     _paymentService;

    public GetTopUpPaymentLinkHandler(
        IUserTransactionRepository transactionRepository,
        IPaymentGatewayService     paymentService)
    {
        _transactionRepository = transactionRepository;
        _paymentService        = paymentService;
    }

    public async Task<BaseResponse<TopUpPaymentLinkDTO>> Handle(
        GetTopUpPaymentLinkQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tx = await _transactionRepository.GetByIdAsync(request.TransactionId);

            if (tx is null)
                return BaseResponse<TopUpPaymentLinkDTO>.Fail("Транзакция не найдена.");

            if (tx.UserId != request.UserId)
                return BaseResponse<TopUpPaymentLinkDTO>.Fail("Нет доступа к этой транзакции.");

            if (tx.TransactionType != TransactionType.UpBalance)
                return BaseResponse<TopUpPaymentLinkDTO>.Fail("Ссылка доступна только для пополнений.");

            if (tx.TransactionStatus != TransactionStatus.Pending)
                return BaseResponse<TopUpPaymentLinkDTO>.Fail("Пополнение уже завершено или отменено.");

            // Внутренние транзакции (не через OxaPay) ссылки не имеют
            if (tx.TrackId.StartsWith("balance-") || tx.TrackId.StartsWith("transfer-"))
                return BaseResponse<TopUpPaymentLinkDTO>.Fail("Это внутренняя транзакция.");

            var invoice = await _paymentService.GetInvoiceAsync(tx.TrackId);

            if (invoice.Status is "Expired" or "Failed")
                return BaseResponse<TopUpPaymentLinkDTO>.Fail(
                    "Срок оплаты истёк. Создайте новое пополнение.");

            return BaseResponse<TopUpPaymentLinkDTO>.Success(new TopUpPaymentLinkDTO
            {
                PaymentUrl = invoice.PaymentUrl,
                ExpiredAt  = invoice.ExpiredAt,
            });
        }
        catch (Exception)
        {
            return BaseResponse<TopUpPaymentLinkDTO>.Fail("Не удалось получить ссылку на оплату.");
        }
    }
}

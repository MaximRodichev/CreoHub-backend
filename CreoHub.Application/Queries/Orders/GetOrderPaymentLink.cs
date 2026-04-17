using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

public record GetOrderPaymentLinkQuery(Guid UserId, Guid OrderId)
    : IRequest<BaseResponse<GetOrderPaymentLinkDTO>>;

public class GetOrderPaymentLinkDTO
{
    public string PaymentUrl { get; init; } = string.Empty;
    public DateTime ExpiredAt { get; init; }
}

public class GetOrderPaymentLinkHandler
    : IRequestHandler<GetOrderPaymentLinkQuery, BaseResponse<GetOrderPaymentLinkDTO>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGatewayService _paymentService;

    public GetOrderPaymentLinkHandler(
        IOrderRepository orderRepository,
        IPaymentGatewayService paymentService)
    {
        _orderRepository = orderRepository;
        _paymentService  = paymentService;
    }

    public async Task<BaseResponse<GetOrderPaymentLinkDTO>> Handle(
        GetOrderPaymentLinkQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId);

        if (order is null)
            return BaseResponse<GetOrderPaymentLinkDTO>.Fail("Заказ не найден.");

        if (order.CustomerId != request.UserId)
            return BaseResponse<GetOrderPaymentLinkDTO>.Fail("Нет доступа к этому заказу.");

        if (order.Status != OrderStatus.Created)
            return BaseResponse<GetOrderPaymentLinkDTO>.Fail(
                "Ссылка на оплату доступна только для заказов в статусе Created.");

        if (order.Transaction is null)
            return BaseResponse<GetOrderPaymentLinkDTO>.Fail("Транзакция для этого заказа не найдена.");

        // balance-orders have a synthetic trackId starting with "balance-"
        if (order.Transaction.TrackId.StartsWith("balance-"))
            return BaseResponse<GetOrderPaymentLinkDTO>.Fail(
                "Этот заказ оплачен через внутренний баланс.");

        var invoice = await _paymentService.GetInvoiceAsync(order.Transaction.TrackId);

        if (invoice.Status is "Expired" or "Failed")
            return BaseResponse<GetOrderPaymentLinkDTO>.Fail(
                "Инвойс истёк или отменён. Создайте новый заказ.");

        return BaseResponse<GetOrderPaymentLinkDTO>.Success(new GetOrderPaymentLinkDTO
        {
            PaymentUrl = invoice.PaymentUrl,
            ExpiredAt  = invoice.ExpiredAt
        });
    }
}

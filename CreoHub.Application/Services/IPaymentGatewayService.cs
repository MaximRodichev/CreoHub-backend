using CreoHub.Application.DTO.PaymentDTOs;

namespace CreoHub.Application.Services;

public interface IPaymentGatewayService
{
    Task<CreateInvoiceResult> CreateInvoiceAsync(decimal amount, string orderId);
    Task<CreatePayoutResult> CreatePayoutAsync(decimal amount, string address, string network);
}
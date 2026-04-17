using CreoHub.Application.DTO.PaymentDTOs;

namespace CreoHub.Application.Services;

public interface IPaymentGatewayService
{
    Task<CreateInvoiceResult> CreateInvoiceAsync(decimal amount, string orderId);
    Task<CreatePayoutResult> CreatePayoutAsync(decimal amount, string address, string network);
    /// <summary>Получить актуальные данные инвойса по trackId (в т.ч. свежий paymentUrl).</summary>
    Task<GetInvoiceResult> GetInvoiceAsync(string trackId);
}

public record GetInvoiceResult(string PaymentUrl, string Status, DateTime ExpiredAt);
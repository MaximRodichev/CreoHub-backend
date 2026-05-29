using CreoHub.Application.DTO.PaymentDTOs;
using CreoHub.Application.Services;

namespace CreoHub.IntegrationTests.Infrastructure;

public sealed class FakePaymentGatewayService : IPaymentGatewayService
{
    public Task<CreateInvoiceResult> CreateInvoiceAsync(decimal amount, string orderId)
        => Task.FromResult(new CreateInvoiceResult(
            TrackId: $"invoice-{orderId}",
            PaymentUrl: $"https://payments.integration.test/invoices/{orderId}",
            ExpiredAt: DateTime.UtcNow.AddMinutes(30)));

    public Task<CreatePayoutResult> CreatePayoutAsync(decimal amount, string address, string network)
        => Task.FromResult(new CreatePayoutResult($"payout-{Guid.NewGuid():N}"));

    public Task<GetInvoiceResult> GetInvoiceAsync(string trackId)
        => Task.FromResult(new GetInvoiceResult(
            PaymentUrl: $"https://payments.integration.test/invoices/{trackId}",
            Status: "Waiting",
            ExpiredAt: DateTime.UtcNow.AddMinutes(30)));
}

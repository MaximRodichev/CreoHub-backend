using System.Net.Http.Json;
using CreoHub.Application.DTO.PaymentDTOs;
using CreoHub.Application.Services;
using CreoHub.Infrastructure.Persistence.DTOs.OxaPay;
using Microsoft.Extensions.Configuration;

namespace CreoHub.Infrastructure.Persistence.Services;

public class OxaPayService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly string _merchantApiKey;
    private readonly string _payoutApiKey;
    private readonly string _callbackUrl;
    private readonly string _returnUrl;
    private readonly bool _sandbox;

    public OxaPayService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _merchantApiKey = config["OxaPay:MerchantApiKey"]!;
        _payoutApiKey = config["OxaPay:PayoutApiKey"]!;
        _callbackUrl = config["OxaPay:CallbackUrl"]!;
        _returnUrl = config["OxaPay:ReturnUrl"]!;
        _sandbox = bool.Parse(config["OxaPay:Sandbox"] ?? "false");
    }

    public async Task<CreateInvoiceResult> CreateInvoiceAsync(decimal amount, string orderId)
    {
        var payload = new
        {
            amount,
            currency = "USD",
            lifetime = 30,
            order_id = orderId,
            callback_url = _callbackUrl,
            return_url = _returnUrl,
            sandbox = _sandbox
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("merchant_api_key", _merchantApiKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.oxapay.com/v1/payment/invoice", payload);

        var result = await response.Content.ReadFromJsonAsync<OxaPayInvoiceResponse>();

        return new CreateInvoiceResult(
            result!.Data.TrackId,
            result.Data.PaymentUrl,
            DateTimeOffset.FromUnixTimeSeconds(result.Data.ExpiredAt).UtcDateTime
        );
    }

    public async Task<CreatePayoutResult> CreatePayoutAsync(
        decimal amount, string address, string network)
    {
        var payload = new
        {
            amount,
            currency = "USDT",
            network,
            address
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("payout_api_key", _payoutApiKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.oxapay.com/v1/payout", payload);

        var result = await response.Content.ReadFromJsonAsync<OxaPayPayoutResponse>();

        return new CreatePayoutResult(result!.Data.TrackId);
    }

    public async Task<GetInvoiceResult> GetInvoiceAsync(string trackId)
    {
        var payload = new { trackId };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("merchant_api_key", _merchantApiKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.oxapay.com/v1/payment/inquiry", payload);

        var result = await response.Content.ReadFromJsonAsync<OxaPayInquiryResponse>();

        return new GetInvoiceResult(
            PaymentUrl: result!.Data.PaymentUrl,
            Status:     result.Data.Status,
            ExpiredAt:  DateTimeOffset.FromUnixTimeSeconds(result.Data.ExpiredAt).UtcDateTime
        );
    }
}
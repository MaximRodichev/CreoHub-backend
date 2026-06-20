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
    private readonly string _payoutCallbackUrl;
    private readonly bool _sandbox;

    public OxaPayService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _merchantApiKey = config["OxaPay:MerchantApiKey"]!;
        _payoutApiKey = config["OxaPay:PayoutApiKey"]!;
        _callbackUrl = config["OxaPay:CallbackUrl"]!;
        _payoutCallbackUrl = config["OxaPay:PayoutCallbackUrl"]!;
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
            currency    = "USDT",
            network,
            address,
            callback_url = _payoutCallbackUrl,
            sandbox      = _sandbox,
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("payout_api_key", _payoutApiKey);

        var httpResponse = await _httpClient.PostAsJsonAsync(
            "https://api.oxapay.com/v1/payout", payload);

        var result = await httpResponse.Content.ReadFromJsonAsync<OxaPayPayoutResponse>();

        if (result is null)
            throw new InvalidOperationException("OxaPay вернул пустой ответ.");

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"OxaPay отклонил запрос: {result.Message} (code {result.Result})");

        if (result.Data?.TrackId is null)
            throw new InvalidOperationException("OxaPay не вернул track_id.");

        return new CreatePayoutResult(result.Data.TrackId);
    }

    public async Task<GetInvoiceResult> GetInvoiceAsync(string trackId)
    {
        // OxaPay v1 ожидает snake_case (как order_id/callback_url в CreateInvoice).
        // Раньше слалось { trackId } → инвойс не находился → Data == null → NRE.
        var payload = new { track_id = trackId };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("merchant_api_key", _merchantApiKey);

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.oxapay.com/v1/payment/inquiry", payload);

        var result = await response.Content.ReadFromJsonAsync<OxaPayInquiryResponse>();

        if (result?.Data is null)
            throw new InvalidOperationException(
                $"OxaPay inquiry вернул пустой ответ (HTTP {(int)response.StatusCode}) для track_id={trackId}.");

        return new GetInvoiceResult(
            PaymentUrl: result.Data.PaymentUrl,
            Status:     result.Data.Status,
            ExpiredAt:  DateTimeOffset.FromUnixTimeSeconds(result.Data.ExpiredAt).UtcDateTime
        );
    }
}
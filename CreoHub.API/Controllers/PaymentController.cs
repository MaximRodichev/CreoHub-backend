using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CreoHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IPaymentGatewayService _paymentService;
    private readonly IConfiguration _config;
    
    public PaymentController(ILogger<PaymentController> logger,  IPaymentGatewayService paymentService,  IConfiguration config)
    {
        _logger = logger;
        _paymentService = paymentService;
        _config = config;
    }
    
    
    
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] JsonElement body)
    {
        // 1. Получаем подпись из заголовка
        var hmacHeader = Request.Headers["hmac"].ToString();
    
        // 2. Берём сырой body
        var rawBody = body.ToString();
    
        // 3. Считаем свою подпись
        var key = Encoding.UTF8.GetBytes(_config["OxaPay:MerchantApiKey"]!);
        var data = Encoding.UTF8.GetBytes(rawBody);
        using var hmac = new HMACSHA512(key);
        var calculated = Convert.ToHexString(hmac.ComputeHash(data)).ToLower();
    
        // 4. Сравниваем
        if (calculated != hmacHeader.ToLower())
        {
            _logger.LogWarning("Invalid HMAC signature");
            return BadRequest("Invalid signature");
        }
    
        _logger.LogInformation("OxaPay webhook: {Body}", rawBody);
        return Ok("ok");
    }
    
    [AllowAnonymous]
    [HttpGet("test-invoice")]
    public async Task<IActionResult> TestInvoice()
    {
        var result = await _paymentService.CreateInvoiceAsync(10, "test-order-123");
        return Ok(new { result.PaymentUrl, result.TrackId });
    }
    
}
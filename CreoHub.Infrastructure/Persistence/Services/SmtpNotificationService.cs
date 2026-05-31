using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CreoHub.Infrastructure.Persistence.Services;

/// <summary>
/// Sends email notifications via SMTP (configured under "Smtp:" in appsettings).
/// Keys: Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password, Smtp:FromAddress, Smtp:FromName
/// </summary>
public class SmtpNotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpNotificationService> _logger;

    public SmtpNotificationService(
        IConfiguration configuration,
        ILogger<SmtpNotificationService> logger)
    {
        _config = configuration;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host)) return false;

        try
        {
            int.TryParse(_config["Smtp:Port"], out var port);
            if (port == 0) port = 587;

            var fromAddress = _config["Smtp:FromAddress"] ?? _config["Smtp:Username"];
            var fromName    = _config["Smtp:FromName"] ?? "CreoHub";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl             = true,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials           = new NetworkCredential(
                    _config["Smtp:Username"],
                    _config["Smtp:Password"]),
            };

            using var message = new MailMessage
            {
                From    = new MailAddress(fromAddress!, fromName),
                Subject = subject,
                Body    = body,
                IsBodyHtml = false,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP notification failed for {Email}", toEmail);
            return false;
        }
    }
}

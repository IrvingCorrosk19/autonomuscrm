using System.Net;
using System.Net.Mail;
using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

/// <summary>Amazon SES via SMTP interface (IAM SMTP credentials).</summary>
public sealed class SesEmailDeliveryProvider : IEmailDeliveryProvider
{
    private readonly CommunicationOptions _options;
    private readonly ILogger<SesEmailDeliveryProvider> _logger;

    public SesEmailDeliveryProvider(IOptions<CommunicationOptions> options, ILogger<SesEmailDeliveryProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendAsync(
        string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SesAccessKey) || string.IsNullOrWhiteSpace(_options.SesSecretKey))
            return (false, "SES SMTP credentials not configured");

        var region = _options.SesRegion ?? "us-east-1";
        var host = $"email-smtp.{region}.amazonaws.com";

        try
        {
            using var client = new SmtpClient(host, 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_options.SesAccessKey, _options.SesSecretKey)
            };
            using var message = new MailMessage(_options.FromAddress, to, subject, body) { IsBodyHtml = true };
            message.From = new MailAddress(_options.FromAddress, _options.FromName);
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("SES SMTP email sent to {To}", to);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SES send failed");
            return (false, ex.Message);
        }
    }
}

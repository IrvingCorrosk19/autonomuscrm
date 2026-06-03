using System.Net;
using System.Net.Mail;
using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public sealed class SmtpEmailDeliveryProvider : IEmailDeliveryProvider
{
    private readonly CommunicationOptions _options;
    private readonly ILogger<SmtpEmailDeliveryProvider> _logger;

    public SmtpEmailDeliveryProvider(IOptions<CommunicationOptions> options, ILogger<SmtpEmailDeliveryProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendAsync(
        string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            return (false, "SMTP host not configured");

        try
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.SmtpUseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
                client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = body.Contains("<html", StringComparison.OrdinalIgnoreCase)
            };
            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("SMTP email sent to {To}", to);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed to {To}", to);
            return (false, ex.Message);
        }
    }
}

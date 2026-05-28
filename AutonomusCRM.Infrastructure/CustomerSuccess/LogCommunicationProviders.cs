using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class LogEmailDeliveryProvider : IEmailDeliveryProvider
{
    private readonly ILogger<LogEmailDeliveryProvider> _logger;

    public LogEmailDeliveryProvider(ILogger<LogEmailDeliveryProvider> logger) => _logger = logger;

    public Task<(bool Success, string? Error)> SendAsync(
        string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email sent to {To} | Subject: {Subject} | Length: {Len}", to, subject, body.Length);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}

public class LogWhatsAppDeliveryProvider : IWhatsAppDeliveryProvider
{
    private readonly ILogger<LogWhatsAppDeliveryProvider> _logger;

    public LogWhatsAppDeliveryProvider(ILogger<LogWhatsAppDeliveryProvider> logger) => _logger = logger;

    public Task<(bool Success, string? Error)> SendAsync(
        string phone, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp sent to {Phone} | Message length: {Len}", phone, message.Length);
        return Task.FromResult<(bool, string?)>((true, null));
    }
}

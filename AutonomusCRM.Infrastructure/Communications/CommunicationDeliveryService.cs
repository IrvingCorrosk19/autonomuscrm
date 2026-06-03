using AutonomusCRM.Application.Communications;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.Communications;

public sealed class CommunicationDeliveryService : ICommunicationDeliveryService
{
    private const int MaxAttempts = 3;
    private readonly IEmailDeliveryProvider _email;
    private readonly IWhatsAppDeliveryProvider _whatsApp;
    private readonly ICustomerCommunicationRepository _logs;
    private readonly IUnitOfWork _uow;
    private readonly CommunicationOptions _options;
    private readonly IOperationalGraphFeed _graphFeed;
    private readonly ILogger<CommunicationDeliveryService> _logger;

    public CommunicationDeliveryService(
        IEmailDeliveryProvider email,
        IWhatsAppDeliveryProvider whatsApp,
        ICustomerCommunicationRepository logs,
        IUnitOfWork uow,
        IOptions<CommunicationOptions> options,
        IOperationalGraphFeed graphFeed,
        ILogger<CommunicationDeliveryService> logger)
    {
        _email = email;
        _whatsApp = whatsApp;
        _logs = logs;
        _uow = uow;
        _options = options.Value;
        _graphFeed = graphFeed;
        _logger = logger;
    }

    public Task<CommunicationSendResult> SendEmailAsync(CommunicationSendRequest request, CancellationToken cancellationToken = default)
        => SendWithRetryAsync("Email", request, async () =>
        {
            var (ok, err) = await _email.SendAsync(request.Recipient, request.Subject, request.Body, cancellationToken);
            return (ok, err);
        }, cancellationToken);

    public Task<CommunicationSendResult> SendWhatsAppAsync(CommunicationSendRequest request, CancellationToken cancellationToken = default)
        => SendWithRetryAsync("WhatsApp", request, async () =>
        {
            var (ok, err) = await _whatsApp.SendAsync(request.Recipient, request.Body, cancellationToken);
            return (ok, err);
        }, cancellationToken);

    private async Task<CommunicationSendResult> SendWithRetryAsync(
        string channel,
        CommunicationSendRequest request,
        Func<Task<(bool Success, string? Error)>> send,
        CancellationToken cancellationToken)
    {
        EnsureNotSimulatedInProduction(channel);

        var log = CustomerCommunicationLog.CreateQueued(
            request.TenantId, channel, request.EventType ?? "manual", request.TemplateKey ?? "direct",
            request.Recipient, request.CustomerId, request.LeadId);
        await _logs.AddAsync(log, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        string? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var (ok, err) = await send();
                if (ok)
                {
                    log.MarkSent();
                    await _logs.UpdateAsync(log, cancellationToken);
                    await _uow.SaveChangesAsync(cancellationToken);
                    await _graphFeed.RecordCommunicationAsync(request.TenantId, log.Id, channel, request.CustomerId, null, cancellationToken);
                    return new CommunicationSendResult(true, log.TrackingId, null, attempt);
                }

                lastError = err ?? "Unknown delivery error";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(ex, "Comms attempt {Attempt} failed {Channel}", attempt, channel);
            }

            if (attempt < MaxAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
        }

        log.MarkFailed(lastError ?? "failed");
        await _logs.UpdateAsync(log, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new CommunicationSendResult(false, log.TrackingId, lastError, MaxAttempts);
    }

    private void EnsureNotSimulatedInProduction(string channel)
    {
        if (_options.AllowSimulation) return;
        var isLog = channel == "Email"
            ? string.Equals(_options.EmailProvider, "Log", StringComparison.OrdinalIgnoreCase)
            : !string.Equals(_options.WhatsAppProvider, "WhatsAppBusiness", StringComparison.OrdinalIgnoreCase);
        if (isLog)
            throw new InvalidOperationException($"Production comms require live provider; {channel} is simulated.");
    }
}

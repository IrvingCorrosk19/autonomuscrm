using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class EmailAutomationEngine : IEmailAutomationEngine
{
    private readonly ICustomerCommunicationRepository _logRepository;
    private readonly IEmailDeliveryProvider _provider;
    private readonly IUnitOfWork _unitOfWork;

    public EmailAutomationEngine(
        ICustomerCommunicationRepository logRepository,
        IEmailDeliveryProvider provider,
        IUnitOfWork unitOfWork)
    {
        _logRepository = logRepository;
        _provider = provider;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommunicationSendResultDto> SendTemplatedAsync(
        Guid tenantId,
        string eventType,
        string templateKey,
        string recipient,
        Guid? customerId = null,
        Guid? leadId = null,
        IReadOnlyDictionary<string, string>? variables = null,
        CancellationToken cancellationToken = default)
    {
        if (!CommunicationTemplates.EmailTemplates.TryGetValue(templateKey, out var tpl))
            tpl = ("Notificación AutonomusFlow", "Mensaje del sistema.");

        var body = CommunicationTemplates.Render(tpl.Body, variables);
        var subject = CommunicationTemplates.Render(tpl.Subject, variables);

        var log = CustomerCommunicationLog.CreateQueued(
            tenantId,
            CustomerSuccessConstants.ChannelEmail,
            eventType,
            templateKey,
            recipient,
            customerId,
            leadId,
            variables?.ToDictionary(kv => kv.Key, kv => (object)kv.Value));

        await _logRepository.AddAsync(log, cancellationToken);

        var (ok, err) = await _provider.SendAsync(recipient, subject, body, cancellationToken);
        if (ok)
            log.MarkSent();
        else
            log.MarkFailed(err ?? "Unknown error");

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CommunicationSendResultDto(log.Id, log.TrackingId ?? "", log.Status, log.Channel, log.EventType);
    }
}

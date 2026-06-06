using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public class WhatsAppAutomationEngine : IWhatsAppAutomationEngine
{
    private readonly ICustomerCommunicationRepository _logRepository;
    private readonly IWhatsAppDeliveryProvider _provider;
    private readonly IUnitOfWork _unitOfWork;

    public WhatsAppAutomationEngine(
        ICustomerCommunicationRepository logRepository,
        IWhatsAppDeliveryProvider provider,
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
        string recipientPhone,
        Guid? customerId = null,
        IReadOnlyDictionary<string, string>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
        var tpl = CommunicationTemplates.GetWhatsAppTemplate(templateKey, culture);
        var message = CommunicationTemplates.Render(tpl, variables);

        var log = CustomerCommunicationLog.CreateQueued(
            tenantId,
            CustomerSuccessConstants.ChannelWhatsApp,
            eventType,
            templateKey,
            recipientPhone,
            customerId,
            null,
            variables?.ToDictionary(kv => kv.Key, kv => (object)kv.Value));

        await _logRepository.AddAsync(log, cancellationToken);

        var (ok, err) = await _provider.SendAsync(recipientPhone, message, cancellationToken);
        if (ok)
            log.MarkSent();
        else
            log.MarkFailed(err ?? "Unknown error");

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CommunicationSendResultDto(log.Id, log.TrackingId ?? "", log.Status, log.Channel, log.EventType);
    }
}

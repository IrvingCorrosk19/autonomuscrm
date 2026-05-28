using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.CustomerSuccess;

public class CustomerCommunicationLog : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? LeadId { get; private set; }
    public string Channel { get; private set; }
    public string EventType { get; private set; }
    public string TemplateKey { get; private set; }
    public string Recipient { get; private set; }
    public string Status { get; private set; }
    public string? TrackingId { get; private set; }
    public Dictionary<string, object> Variables { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private CustomerCommunicationLog() : base()
    {
        Channel = string.Empty;
        EventType = string.Empty;
        TemplateKey = string.Empty;
        Recipient = string.Empty;
        Status = "Queued";
        Variables = new Dictionary<string, object>();
    }

    public static CustomerCommunicationLog CreateQueued(
        Guid tenantId,
        string channel,
        string eventType,
        string templateKey,
        string recipient,
        Guid? customerId = null,
        Guid? leadId = null,
        Dictionary<string, object>? variables = null)
    {
        return new CustomerCommunicationLog
        {
            TenantId = tenantId,
            CustomerId = customerId,
            LeadId = leadId,
            Channel = channel,
            EventType = eventType,
            TemplateKey = templateKey,
            Recipient = recipient,
            Variables = variables ?? new Dictionary<string, object>(),
            Status = "Queued",
            TrackingId = Guid.NewGuid().ToString("N")[..16]
        };
    }

    public void MarkSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error;
        MarkAsUpdated();
    }
}

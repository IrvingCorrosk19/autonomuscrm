namespace AutonomusCRM.Application.Communications;

public record CommunicationSendRequest(
    Guid TenantId,
    string Channel,
    string Recipient,
    string Subject,
    string Body,
    Guid? CustomerId,
    Guid? LeadId,
    string? TemplateKey,
    string? EventType);

public record CommunicationSendResult(bool Success, string? TrackingId, string? Error, int Attempts);

public interface ICommunicationDeliveryService
{
    Task<CommunicationSendResult> SendEmailAsync(CommunicationSendRequest request, CancellationToken cancellationToken = default);
    Task<CommunicationSendResult> SendWhatsAppAsync(CommunicationSendRequest request, CancellationToken cancellationToken = default);
}

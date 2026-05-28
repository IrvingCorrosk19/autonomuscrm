namespace AutonomusCRM.Application.CustomerSuccess;

public interface IEmailDeliveryProvider
{
    Task<(bool Success, string? Error)> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}

public interface IWhatsAppDeliveryProvider
{
    Task<(bool Success, string? Error)> SendAsync(
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}

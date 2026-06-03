using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public sealed class CommunicationStatusService : ICommunicationStatusService
{
    private readonly CommunicationOptions _options;

    public CommunicationStatusService(IOptions<CommunicationOptions> options) => _options = options.Value;

    public CommunicationStatusDto GetStatus()
    {
        var emailProvider = (_options.EmailProvider ?? "Log").Trim();
        var emailLive = !string.Equals(emailProvider, "Log", StringComparison.OrdinalIgnoreCase);
        var waProvider = (_options.WhatsAppProvider ?? "Log").Trim();
        var waLive = string.Equals(waProvider, "WhatsAppBusiness", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_options.WhatsAppAccessToken);

        var simulated = new List<string>();
        if (!emailLive) simulated.Add("Email");
        if (!waLive) simulated.Add("WhatsApp");
        var warning = simulated.Count > 0
            ? $"MODO SIMULACIÓN ({string.Join(", ", simulated)}): no sale a clientes reales hasta configurar proveedores en producción."
            : string.Empty;

        return new CommunicationStatusDto(
            emailLive ? emailProvider : "Log (simulado)",
            emailLive,
            waLive ? "WhatsAppBusiness" : "Log (simulado)",
            waLive,
            warning);
    }
}

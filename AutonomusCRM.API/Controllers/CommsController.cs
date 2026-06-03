using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Communications;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/comms")]
public class CommsController : ControllerBase
{
    private readonly ICommunicationDeliveryService _delivery;
    private readonly ITenantContext _tenant;

    public CommsController(ICommunicationDeliveryService delivery, ITenantContext tenant)
    {
        _delivery = delivery;
        _tenant = tenant;
    }

    [HttpPost("email")]
    public async Task<IActionResult> SendEmail([FromBody] CommsSendDto dto, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var result = await _delivery.SendEmailAsync(new CommunicationSendRequest(
            tenantId, "Email", dto.To, dto.Subject, dto.Body, dto.CustomerId, null, dto.TemplateKey, dto.EventType),
            cancellationToken);
        return result.Success ? Ok(result) : StatusCode(502, result);
    }

    [HttpPost("whatsapp")]
    public async Task<IActionResult> SendWhatsApp([FromBody] CommsWhatsAppDto dto, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var result = await _delivery.SendWhatsAppAsync(new CommunicationSendRequest(
            tenantId, "WhatsApp", dto.To, "", dto.Body, dto.CustomerId, null, dto.TemplateKey, dto.EventType),
            cancellationToken);
        return result.Success ? Ok(result) : StatusCode(502, result);
    }
}

public record CommsSendDto(string To, string Subject, string Body, Guid? CustomerId, string? TemplateKey, string? EventType);
public record CommsWhatsAppDto(string To, string Body, Guid? CustomerId, string? TemplateKey, string? EventType);

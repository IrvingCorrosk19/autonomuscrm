using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class UnderstandModel : PageModel
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IServiceProvider _sp;

    public UnderstandModel(
        IDbConnectionProfileService connections,
        IBusinessDiscoveryService businessDiscovery,
        IServiceProvider sp)
    {
        _connections = connections;
        _businessDiscovery = businessDiscovery;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    [BindProperty]
    public Guid MappingId { get; set; }

    [BindProperty]
    public string? Action { get; set; }

    [BindProperty]
    public BusinessEntityType? CorrectedEntityType { get; set; }

    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public BusinessDiscoveryResultDto? DiscoveryResult { get; private set; }
    public IReadOnlyList<DbTableBusinessMappingDto> Mappings { get; private set; } = Array.Empty<DbTableBusinessMappingDto>();
    public string? StatusMessage { get; private set; }
    public IReadOnlyList<BusinessEntityType> EntityTypes { get; } =
        Enum.GetValues<BusinessEntityType>().Where(e => e != BusinessEntityType.Unknown).ToList();

    public string EntityLabel(BusinessEntityType type) => type switch
    {
        BusinessEntityType.Customer => "Cliente",
        BusinessEntityType.Company => "Empresa",
        BusinessEntityType.Contact => "Contacto",
        BusinessEntityType.Sale => "Venta",
        BusinessEntityType.Invoice => "Factura",
        BusinessEntityType.Payment => "Pago",
        BusinessEntityType.Product => "Producto",
        BusinessEntityType.Activity => "Actividad",
        BusinessEntityType.User => "Usuario",
        _ => "Desconocido"
    };

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetAnalyzeAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            StatusMessage = "Only administrators can start business analysis.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            DiscoveryResult = await _businessDiscovery.RunBusinessDiscoveryAsync(
                tenantId,
                Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty,
                ConnectionId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            StatusMessage = $"Analysis completed — {DiscoveryResult.EntitiesDetected} business entities detected.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var action = Action?.Trim() ?? string.Empty;
        if (action.Equals("Correct", StringComparison.OrdinalIgnoreCase) && CorrectedEntityType == null)
        {
            StatusMessage = "Select an entity type to correct.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        try
        {
            await _businessDiscovery.ConfirmMappingAsync(
                tenantId,
                Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty,
                new ConfirmBusinessMappingRequest(MappingId, action, CorrectedEntityType),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            StatusMessage = "Mapping updated.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        ConnectionId = SelectedConnectionId != Guid.Empty ? SelectedConnectionId : ConnectionId;
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Connections = await _connections.ListAsync(tenantId, cancellationToken);
        SelectedConnectionId = ConnectionId != Guid.Empty ? ConnectionId : Connections.FirstOrDefault()?.Id ?? Guid.Empty;
        if (SelectedConnectionId == Guid.Empty) return;

        DiscoveryResult = await _businessDiscovery.GetLatestBusinessDiscoveryAsync(tenantId, SelectedConnectionId, cancellationToken);
        Mappings = await _businessDiscovery.ListMappingsAsync(tenantId, SelectedConnectionId, cancellationToken);
    }
}

using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class HealthModel : PageModel
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IDataHealthService _health;
    private readonly IServiceProvider _sp;

    public HealthModel(IDbConnectionProfileService connections, IDataHealthService health, IServiceProvider sp)
    {
        _connections = connections;
        _health = health;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public DataHealthResultDto? Result { get; private set; }
    public string? StatusMessage { get; private set; }

    public string EntityLabel(BusinessEntityType type) => type switch
    {
        BusinessEntityType.Customer => "Customers",
        BusinessEntityType.Company => "Companies",
        BusinessEntityType.Contact => "Contacts",
        BusinessEntityType.Invoice => "Invoices",
        BusinessEntityType.Payment => "Payments",
        BusinessEntityType.Product => "Products",
        BusinessEntityType.Sale => "Sales",
        BusinessEntityType.Activity => "Activities",
        _ => type.ToString()
    };

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetScanAsync(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            StatusMessage = "Only administrators can start a health scan.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            Result = await _health.RunHealthScanAsync(
                tenantId,
                Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty,
                ConnectionId,
                DataHealthScanMode.Full,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            StatusMessage = $"Health scan completed — score {Result.Job.GlobalScore} ({Result.Job.GlobalBand}).";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            await LoadAsync(cancellationToken);
        }

        if (Result == null)
            await LoadAsync(cancellationToken);
        else
        {
            Connections = await _connections.ListAsync(tenantId, cancellationToken);
            SelectedConnectionId = ConnectionId;
        }

        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Connections = await _connections.ListAsync(tenantId, cancellationToken);
        SelectedConnectionId = ConnectionId != Guid.Empty ? ConnectionId : Connections.FirstOrDefault()?.Id ?? Guid.Empty;
        if (SelectedConnectionId == Guid.Empty) return;
        Result = await _health.GetLatestHealthResultAsync(tenantId, SelectedConnectionId, cancellationToken);
    }
}

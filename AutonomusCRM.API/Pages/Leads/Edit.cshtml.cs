using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Application.Leads.Queries;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Leads;

public class EditModel : PageModel
{
    public LeadDto? Lead { get; set; }
    public List<SelectListItem> Sources { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var query = new GetLeadsByTenantQuery(tenantId, null);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var leads = await handler.HandleAsync(query, CancellationToken.None);
            
            Lead = leads.FirstOrDefault(l => l.Id == id);
            
            if (Lead == null)
            {
                return NotFound();
            }

            InitializeSelectLists();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading lead for edit");
            return RedirectToPage("/Leads");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string name, string? email, string? phone, string? company, LeadSource source, LeadStatus? status)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new UpdateLeadCommand(id, tenantId, name, source, email, phone, company, status);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateLeadCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Leads/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lead");
            ModelState.AddModelError("", "Error al actualizar el lead: " + ex.Message);
            
            // Recargar datos
            var tenantId = await GetDefaultTenantIdAsync();
            var query = new GetLeadsByTenantQuery(tenantId, null);
            var queryHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var leads = await queryHandler.HandleAsync(query, CancellationToken.None);
            Lead = leads.FirstOrDefault(l => l.Id == id);
            InitializeSelectLists();
            
            return Page();
        }
    }

    private void InitializeSelectLists()
    {
        Sources = Enum.GetValues<LeadSource>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = s.ToString(),
                Selected = Lead?.Source == s
            }).ToList();

        Statuses = Enum.GetValues<LeadStatus>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = s.ToString(),
                Selected = Lead?.Status == s
            }).ToList();
    }

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        try
        {
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync(CancellationToken.None);
            var tenant = tenants.FirstOrDefault();
            
            if (tenant == null)
            {
                var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand, Guid>>();
                var tenantId = await createHandler.HandleAsync(
                    new AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand("Default Tenant", "default@autonomuscrm.com"),
                    CancellationToken.None);
                return tenantId;
            }
            
            return tenant.Id;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}


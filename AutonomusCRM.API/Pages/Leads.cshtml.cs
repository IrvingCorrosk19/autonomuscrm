using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Queries;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class LeadsModel : PageModel
{
    public List<LeadDto> Leads { get; set; } = new();
    public List<LeadDto> FilteredLeads { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public LeadStatus? FilterStatus { get; set; }
    public LeadSource? FilterSource { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeadsModel> _logger;

    public LeadsModel(IServiceProvider serviceProvider, ILogger<LeadsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool? Created { get; set; }

    public async Task OnGetAsync(bool? created = null, string? search = null, LeadStatus? status = null, LeadSource? source = null, int? bulkUpdated = null, int? imported = null)
    {
        try
        {
            Created = created;
            SearchTerm = search;
            FilterStatus = status;
            FilterSource = source;
            TenantId = await GetDefaultTenantIdAsync();
            
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var query = new GetLeadsByTenantQuery(TenantId, status);
            var leads = await handler.HandleAsync(query);
            Leads = leads.ToList();
            
            // Aplicar filtros
            FilteredLeads = Leads.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                FilteredLeads = FilteredLeads.Where(l => 
                    (l.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Company?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Phone?.Contains(SearchTerm) ?? false)
                );
            }
            
            if (FilterSource.HasValue)
            {
                FilteredLeads = FilteredLeads.Where(l => l.Source == FilterSource.Value);
            }
            
            FilteredLeads = FilteredLeads.ToList();
            
            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
            {
                TempData["Message"] = $"Se actualizaron {bulkUpdated.Value} leads correctamente.";
            }
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = $"Se importaron {imported.Value} leads correctamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading leads");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string? email, string? phone, string? company, string source)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            if (!Enum.TryParse<LeadSource>(source, out var leadSource))
            {
                leadSource = LeadSource.Other;
            }
            
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateLeadCommand, Guid>>();
            var command = new CreateLeadCommand(TenantId, name, leadSource, email, phone, company);
            var leadId = await handler.HandleAsync(command);
            
            return RedirectToPage("/Leads");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lead");
            return Page();
        }
    }

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        try
        {
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync();
            var firstTenant = tenants.FirstOrDefault();
            
            if (firstTenant != null)
                return firstTenant.Id;

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand, Guid>>();
            var createCommand = new AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand("Default Tenant", "Tenant por defecto");
            return await createHandler.HandleAsync(createCommand);
        }
        catch
        {
            return Guid.Empty;
        }
    }
}


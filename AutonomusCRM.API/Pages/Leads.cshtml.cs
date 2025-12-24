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
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LeadsModel> _logger;

    public LeadsModel(IServiceProvider serviceProvider, ILogger<LeadsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>>();
            var query = new GetLeadsByTenantQuery(TenantId);
            var leads = await handler.HandleAsync(query);
            Leads = leads.ToList();
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


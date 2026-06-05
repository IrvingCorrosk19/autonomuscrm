using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Queries;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

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
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LeadsModel(IServiceProvider serviceProvider, ILogger<LeadsModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
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
            var filteredLeads = Leads.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filteredLeads = filteredLeads.Where(l => 
                    (l.Name?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Company?.ToLower().Contains(searchLower) ?? false) ||
                    (l.Phone?.Contains(SearchTerm) ?? false)
                );
            }
            
            if (FilterSource.HasValue)
            {
                filteredLeads = filteredLeads.Where(l => l.Source == FilterSource.Value);
            }
            
            FilteredLeads = filteredLeads.ToList();
            
            FilteredLeads = filteredLeads.ToList();
            
            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
            {
                TempData["Message"] = _localizer["Flash_LeadsUpdated", bulkUpdated.Value].Value;
            }
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = _localizer["Flash_LeadsImported", imported.Value].Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading leads");
        }
    }

    [Authorize(Roles = "Admin,Manager,Sales")]
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
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}


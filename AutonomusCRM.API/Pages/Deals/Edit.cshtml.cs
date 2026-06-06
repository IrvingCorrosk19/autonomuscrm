using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages.Deals;

public class EditModel : PageModel
{
    public DealDto? Deal { get; set; }
    public List<CustomerDto> Customers { get; set; } = new();
    public List<SelectListItem> Stages { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var query = new GetDealsByTenantQuery(tenantId, null);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var deals = await handler.HandleAsync(query, CancellationToken.None);
            
            Deal = deals.FirstOrDefault(d => d.Id == id);
            
            if (Deal == null)
            {
                return NotFound();
            }

            // Cargar clientes
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(tenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();

            InitializeSelectLists();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deal for edit");
            return RedirectToPage("/Deals");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string title, string? description, decimal? amount, Guid? customerId, DealStage? stage, int? probability, DateTime? expectedCloseDate)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new UpdateDealCommand(id, tenantId, title, description, amount, customerId, stage, probability, expectedCloseDate);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateDealCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Deals/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating deal");
            ModelState.AddModelError("", _localizer["Flash_DealUpdateError"].Value);
            
            // Recargar datos
            var tenantId = await GetDefaultTenantIdAsync();
            var query = new GetDealsByTenantQuery(tenantId, null);
            var queryHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var deals = await queryHandler.HandleAsync(query, CancellationToken.None);
            Deal = deals.FirstOrDefault(d => d.Id == id);
            
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(tenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
            
            InitializeSelectLists();
            
            return Page();
        }
    }

    private void InitializeSelectLists()
    {
        Stages = Enum.GetValues<DealStage>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = LocalizedLabels.DealStageLabel(_localizer, s),
                Selected = Deal?.Stage == s
            }).ToList();

        Statuses = Enum.GetValues<DealStatus>()
            .Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = LocalizedLabels.DealStatusLabel(_localizer, s),
                Selected = Deal?.Status == s
            }).ToList();
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}


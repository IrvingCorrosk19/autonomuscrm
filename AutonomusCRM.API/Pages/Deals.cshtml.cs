using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages;

public class DealsModel : PageModel
{
    public List<DealDto> Deals { get; set; } = new();
    public List<DealDto> FilteredDeals { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public AutonomusCRM.Domain.Deals.DealStatus? FilterStatus { get; set; }
    public AutonomusCRM.Domain.Deals.DealStage? FilterStage { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DealsModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DealsModel(IServiceProvider serviceProvider, ILogger<DealsModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public bool? Created { get; set; }
    public decimal Forecast30 { get; set; }
    public decimal Forecast60 { get; set; }
    public decimal Forecast90 { get; set; }
    public double WinRate { get; set; }
    public decimal RevenueClosed { get; set; }

    public async Task OnGetAsync(bool? created = null, string? search = null, AutonomusCRM.Domain.Deals.DealStatus? status = null, AutonomusCRM.Domain.Deals.DealStage? stage = null, int? bulkUpdated = null, int? imported = null)
    {
        try
        {
            Created = created;
            SearchTerm = search;
            FilterStatus = status;
            FilterStage = stage;
            TenantId = await GetDefaultTenantIdAsync();
            
            var dealsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var dealsQuery = new GetDealsByTenantQuery(TenantId, status, stage);
            var deals = await dealsHandler.HandleAsync(dealsQuery);
            Deals = deals.ToList();

            // Para el formulario, necesitamos todos los customers
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
            
            // Aplicar búsqueda adicional
            var filteredDeals = Deals.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filteredDeals = filteredDeals.Where(d => 
                    (d.Title?.ToLower().Contains(searchLower) ?? false)
                );
            }
            
            FilteredDeals = filteredDeals.ToList();

            var now = DateTime.UtcNow;
            var openWithDate = Deals.Where(d => d.Status == DealStatus.Open && d.ExpectedCloseDate.HasValue).ToList();
            decimal Weighted(DealDto d) => d.Amount * (d.Probability ?? 0) / 100m;
            Forecast30 = openWithDate.Where(d => d.ExpectedCloseDate <= now.AddDays(30)).Sum(Weighted);
            Forecast60 = openWithDate.Where(d => d.ExpectedCloseDate > now.AddDays(30) && d.ExpectedCloseDate <= now.AddDays(60)).Sum(Weighted);
            Forecast90 = openWithDate.Where(d => d.ExpectedCloseDate > now.AddDays(60) && d.ExpectedCloseDate <= now.AddDays(90)).Sum(Weighted);

            var won = Deals.Count(d => d.Stage == DealStage.ClosedWon);
            var lost = Deals.Count(d => d.Stage == DealStage.ClosedLost);
            WinRate = (won + lost) > 0 ? won * 100.0 / (won + lost) : 0;
            RevenueClosed = Deals.Where(d => d.Stage == DealStage.ClosedWon).Sum(d => d.Amount);
            
            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
            {
                TempData["Message"] = _localizer["Flash_DealsUpdated", bulkUpdated.Value].Value;
            }
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = _localizer["Flash_DealsImported", imported.Value].Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deals");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid customerId, string title, decimal amount, string? description)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
            var command = new CreateDealCommand(TenantId, customerId, title, amount, description);
            var dealId = await handler.HandleAsync(command);
            
            return RedirectToPage("/Deals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal");
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}


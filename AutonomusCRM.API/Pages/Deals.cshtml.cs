using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Application.Deals.Queries;
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
    public List<DealDto> FilteredDeals { get; set; } = new();
    public List<CustomerDto> Customers { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public DealStatus? FilterStatus { get; set; }
    public DealStage? FilterStage { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

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

    public async Task OnGetAsync(
        bool? created = null,
        string? search = null,
        DealStatus? status = null,
        DealStage? stage = null,
        int? bulkUpdated = null,
        int? imported = null,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            Created = created;
            SearchTerm = search;
            FilterStatus = status;
            FilterStage = stage;
            PageIndex = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 200);
            TenantId = await GetDefaultTenantIdAsync();

            var dealRepository = _serviceProvider.GetRequiredService<IDealRepository>();
            var paged = await dealRepository.SearchPagedAsync(TenantId, search, status, stage, PageIndex, PageSize);
            FilteredDeals = paged.Items.Select(d => new DealDto(
                d.Id, d.TenantId, d.CustomerId, d.Title, d.Amount, d.Status, d.Stage,
                d.Probability, d.ExpectedCloseDate, d.CreatedAt, d.Version
            )).ToList();
            TotalCount = paged.TotalCount;
            TotalPages = paged.TotalPages;
            PageIndex = paged.Page;

            var summary = await dealRepository.GetListSummaryAsync(TenantId);
            Forecast30 = summary.Forecast30;
            Forecast60 = summary.Forecast60;
            Forecast90 = summary.Forecast90;
            WinRate = summary.WinRate;
            RevenueClosed = summary.RevenueClosed;

            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();

            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
                TempData["Message"] = _localizer["Flash_DealsUpdated", bulkUpdated.Value].Value;

            if (imported.HasValue && imported.Value > 0)
                TempData["Message"] = _localizer["Flash_DealsImported", imported.Value].Value;
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
            await handler.HandleAsync(new CreateDealCommand(TenantId, customerId, title, amount, description));
            return RedirectToPage("/Deals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal");
            return Page();
        }
    }

    public string BuildPageUrl(int page)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={PageSize}" };
        if (!string.IsNullOrWhiteSpace(SearchTerm))
            parts.Add($"search={Uri.EscapeDataString(SearchTerm)}");
        if (FilterStatus.HasValue)
            parts.Add($"status={(int)FilterStatus.Value}");
        if (FilterStage.HasValue)
            parts.Add($"stage={(int)FilterStage.Value}");
        return "/Deals?" + string.Join("&", parts);
    }

    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

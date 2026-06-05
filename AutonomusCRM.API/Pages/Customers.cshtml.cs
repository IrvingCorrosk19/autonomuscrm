using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Domain.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages;

public class CustomersModel : PageModel
{
    public List<CustomerDto> FilteredCustomers { get; set; } = new();
    public CustomerListSummary Summary { get; set; } = new(0, 0, 0, 0, null, 0);
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public CustomerStatus? FilterStatus { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomersModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CustomersModel(IServiceProvider serviceProvider, ILogger<CustomersModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public bool? Created { get; set; }

    public async Task OnGetAsync(
        bool? created = null,
        string? search = null,
        CustomerStatus? status = null,
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
            PageIndex = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 200);
            TenantId = await GetDefaultTenantIdAsync();

            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var paged = await customerRepository.SearchPagedAsync(TenantId, search, status, PageIndex, PageSize);
            FilteredCustomers = paged.Items.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
            TotalCount = paged.TotalCount;
            TotalPages = paged.TotalPages;
            PageIndex = paged.Page;
            Summary = await customerRepository.GetListSummaryAsync(TenantId);

            if (bulkUpdated.HasValue && bulkUpdated.Value > 0)
                TempData["Message"] = _localizer["Flash_CustomersUpdated", bulkUpdated.Value].Value;

            if (imported.HasValue && imported.Value > 0)
                TempData["Message"] = _localizer["Flash_CustomersImported", imported.Value].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string? email, string? phone, string? company)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateCustomerCommand, Guid>>();
            await handler.HandleAsync(new CreateCustomerCommand(TenantId, name, email, phone, company));
            return RedirectToPage(new { created = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return RedirectToPage();
        }
    }

    public string BuildPageUrl(int page)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={PageSize}" };
        if (!string.IsNullOrWhiteSpace(SearchTerm))
            parts.Add($"search={Uri.EscapeDataString(SearchTerm)}");
        if (FilterStatus.HasValue)
            parts.Add($"status={(int)FilterStatus.Value}");
        return "/Customers?" + string.Join("&", parts);
    }

    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

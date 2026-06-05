using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Application.Customers.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages.Deals;

public class CreateModel : PageModel
{
    [BindProperty]
    public Guid? CustomerId { get; set; }

    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public decimal? Amount { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    public List<CustomerDto> Customers { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid TenantId { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task OnGetAsync(Guid? customerId, string? title, decimal? amount, string? description)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();

            if (customerId.HasValue) CustomerId = customerId;
            if (!string.IsNullOrWhiteSpace(title)) Title = title;
            if (amount.HasValue && amount.Value > 0) Amount = amount;
            if (!string.IsNullOrWhiteSpace(description)) Description = description;
            
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create deal page");
            ErrorMessage = _localizer["Flash_PageLoadError"].Value;
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();

            if (!CustomerId.HasValue)
            {
                ErrorMessage = _localizer["Flash_SelectCustomer"].Value;
                await LoadCustomersAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = _localizer["Flash_TitleRequired"].Value;
                await LoadCustomersAsync();
                return Page();
            }

            if (!Amount.HasValue || Amount.Value <= 0)
            {
                ErrorMessage = _localizer["Flash_AmountPositive"].Value;
                await LoadCustomersAsync();
                return Page();
            }

            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
            var command = new CreateDealCommand(TenantId, CustomerId.Value, Title, Amount.Value, Description);
            var dealId = await handler.HandleAsync(command);

            _logger.LogInformation("Deal creado exitosamente: {DealId}", dealId);

            return RedirectToPage("/Deals", new { created = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deal");
            ErrorMessage = _localizer["Flash_DealCreateError", ex.Message].Value;
            await LoadCustomersAsync();
            return Page();
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(TenantId);
            Customers = customers.Select(c => new CustomerDto(
                c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.Company, c.Status, c.LifetimeValue, c.RiskScore, c.CreatedAt
            )).ToList();
        }
        catch
        {
            Customers = new List<CustomerDto>();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

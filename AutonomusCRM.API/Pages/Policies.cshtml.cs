using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages;

public class PoliciesModel : PageModel
{
    public List<Policy> Policies { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PoliciesModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PoliciesModel(IServiceProvider serviceProvider, ILogger<PoliciesModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task OnGetAsync(int? imported = null)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var policyRepository = _serviceProvider.GetRequiredService<IPolicyRepository>();
            var policies = await policyRepository.GetActiveByTenantAsync(TenantId);
            Policies = policies.ToList();
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = _localizer["Flash_PoliciesImported", imported.Value].Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading policies");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}


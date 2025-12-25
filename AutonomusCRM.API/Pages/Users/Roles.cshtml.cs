using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Users;

public class RolesModel : PageModel
{
    public List<User> Users { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new() { "Admin", "Manager", "Sales", "Support", "Viewer" };
    public Dictionary<string, int> RoleCounts { get; set; } = new();
    public Guid TenantId { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RolesModel> _logger;

    public RolesModel(IServiceProvider serviceProvider, ILogger<RolesModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            var users = await userRepository.GetByTenantIdAsync(TenantId);
            Users = users.ToList();
            
            // Contar usuarios por rol
            foreach (var role in AvailableRoles)
            {
                RoleCounts[role] = Users.Count(u => u.Roles != null && u.Roles.Contains(role));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading roles");
        }
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


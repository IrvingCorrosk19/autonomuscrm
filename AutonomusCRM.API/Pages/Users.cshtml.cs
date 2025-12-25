using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages;

public class UsersModel : PageModel
{
    public List<User> Users { get; set; } = new();
    public Guid TenantId { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(IServiceProvider serviceProvider, ILogger<UsersModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public List<User> FilteredUsers { get; set; } = new();
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(string? search = null, int? imported = null)
    {
        try
        {
            SearchTerm = search;
            TenantId = await GetDefaultTenantIdAsync();
            
            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            var users = await userRepository.GetByTenantIdAsync(TenantId);
            Users = users.ToList();
            
            // Aplicar búsqueda
            var filteredUsers = Users.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filteredUsers = filteredUsers.Where(u => 
                    (u.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (u.FirstName?.ToLower().Contains(searchLower) ?? false) ||
                    (u.LastName?.ToLower().Contains(searchLower) ?? false) ||
                    (u.Roles?.Any(r => r.ToLower().Contains(searchLower)) ?? false)
                );
            }
            
            FilteredUsers = FilteredUsers.ToList();
            
            if (imported.HasValue && imported.Value > 0)
            {
                TempData["Message"] = $"Se importaron {imported.Value} usuarios correctamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
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


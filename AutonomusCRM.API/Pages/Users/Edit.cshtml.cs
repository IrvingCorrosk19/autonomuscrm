using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Users.Commands;
using AutonomusCRM.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Users;

public class EditModel : PageModel
{
    [BindProperty]
    public UpdateUserCommand Command { get; set; } = default!;
    public User? User { get; set; }
    public Guid TenantId { get; set; }
    public List<string> AvailableRoles { get; set; } = new() { "Admin", "Manager", "Sales", "Support", "Viewer" };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            User = await userRepository.GetByIdAsync(id, CancellationToken.None);

            if (User == null || User.TenantId != TenantId)
            {
                return NotFound();
            }

            Command = new UpdateUserCommand(User.Id, TenantId, User.FirstName, User.LastName, User.Email);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user for edit");
            return RedirectToPage("/Users");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            User = await userRepository.GetByIdAsync(Command.UserId, CancellationToken.None);
            return Page();
        }

        try
        {
            var updateHandler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateUserCommand, bool>>();
            var result = await updateHandler.HandleAsync(Command, CancellationToken.None);

            if (result)
            {
                TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                return RedirectToPage("/Users");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "No se pudo actualizar el usuario.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            ModelState.AddModelError(string.Empty, $"Error al actualizar el usuario: {ex.Message}");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAssignRoleAsync(Guid id, string role)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AssignRoleCommand(id, tenantId, role);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AssignRoleCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Users/Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return RedirectToPage("/Users/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostRemoveRoleAsync(Guid id, string role)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new RemoveRoleCommand(id, tenantId, role);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<RemoveRoleCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Users/Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return RedirectToPage("/Users/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, bool isActive)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new ToggleUserStatusCommand(id, tenantId, isActive);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<ToggleUserStatusCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Users/Edit", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status");
            return RedirectToPage("/Users/Edit", new { id });
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

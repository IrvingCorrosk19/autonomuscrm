using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages.Users;

[Authorize(Roles = "Admin,Manager")]
public class CreateModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CreateModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateModel(IServiceProvider serviceProvider, ILogger<CreateModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnPostAsync(string email, string password, string? firstName, string? lastName)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new CreateUserCommand(tenantId, email, password, firstName, lastName);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<CreateUserCommand, Guid>>();
            
            var userId = await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            ModelState.AddModelError("", _localizer["Flash_UserCreateError"].Value);
            return Page();
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}


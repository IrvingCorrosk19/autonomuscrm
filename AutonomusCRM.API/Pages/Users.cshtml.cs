using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Pages;

[Authorize(Roles = "Admin,Manager")]
public class UsersModel : PageModel
{
    public List<User> FilteredUsers { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public UserListSummary Summary { get; set; } = new(0, 0, 0, 0);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UsersModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UsersModel(IServiceProvider serviceProvider, ILogger<UsersModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task OnGetAsync(string? search = null, int? imported = null, int page = 1, int pageSize = 50)
    {
        try
        {
            SearchTerm = search;
            PageIndex = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 200);
            TenantId = await GetDefaultTenantIdAsync();

            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            var paged = await userRepository.SearchPagedAsync(TenantId, search, PageIndex, PageSize);
            FilteredUsers = paged.Items.ToList();
            TotalCount = paged.TotalCount;
            TotalPages = paged.TotalPages;
            PageIndex = paged.Page;
            Summary = await userRepository.GetListSummaryAsync(TenantId);

            if (imported.HasValue && imported.Value > 0)
                TempData["Message"] = _localizer["Flash_UsersImported", imported.Value].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users");
        }
    }

    public string BuildPageUrl(int page)
    {
        var parts = new List<string> { $"page={page}", $"pageSize={PageSize}" };
        if (!string.IsNullOrWhiteSpace(SearchTerm))
            parts.Add($"search={Uri.EscapeDataString(SearchTerm)}");
        return "/Users?" + string.Join("&", parts);
    }

    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Tasks.Commands;
using AutonomusCRM.Application.Tasks.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages;

public class TasksModel : PageModel
{
    public List<WorkflowTaskDto> Tasks { get; set; } = new();
    public List<UserListItem> Users { get; set; } = new();
    public Guid TenantId { get; set; }
    public string? FilterStatus { get; set; }
    public bool? FilterOverdue { get; set; }
    public string? FilterPriority { get; set; }
    public int OpenCount { get; set; }
    public int OverdueCount { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TasksModel> _logger;

    public TasksModel(IServiceProvider serviceProvider, ILogger<TasksModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnGetAsync(string? status = null, bool? overdue = null, string? priority = null)
    {
        try
        {
            FilterStatus = status ?? "Open";
            FilterOverdue = overdue;
            FilterPriority = priority;
            TenantId = await GetTenantIdAsync();

            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetWorkflowTasksQuery, IEnumerable<WorkflowTaskDto>>>();
            Tasks = (await handler.HandleAsync(new GetWorkflowTasksQuery(
                TenantId, FilterStatus, null, FilterOverdue, FilterPriority))).ToList();

            var allOpen = (await handler.HandleAsync(new GetWorkflowTasksQuery(TenantId, "Open"))).ToList();
            OpenCount = allOpen.Count;
            OverdueCount = allOpen.Count(t => t.IsOverdue);

            var userRepo = _serviceProvider.GetRequiredService<IUserRepository>();
            var users = await userRepo.GetByTenantIdAsync(TenantId);
            Users = users.Select(u => new UserListItem(u.Id, u.Email)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tasks");
        }
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid taskId)
    {
        var tenantId = await GetTenantIdAsync();
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<CompleteWorkflowTaskCommand, bool>>();
        await handler.HandleAsync(new CompleteWorkflowTaskCommand(taskId, tenantId));
        return RedirectToPage(new { status = FilterStatus, overdue = FilterOverdue, priority = FilterPriority });
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid taskId, Guid userId)
    {
        var tenantId = await GetTenantIdAsync();
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<AssignWorkflowTaskCommand, bool>>();
        await handler.HandleAsync(new AssignWorkflowTaskCommand(taskId, tenantId, userId));
        return RedirectToPage(new { status = FilterStatus, overdue = FilterOverdue, priority = FilterPriority });
    }

    private Task<Guid> GetTenantIdAsync()
        => this.GetTenantIdForPageAsync(_serviceProvider);
}

public record UserListItem(Guid Id, string Email);

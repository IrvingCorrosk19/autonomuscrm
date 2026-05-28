using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Infrastructure.Revenue;

public class SalesProductivityService : ISalesProductivityService
{
    private readonly IWorkflowTaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;

    public SalesProductivityService(
        IWorkflowTaskRepository taskRepository,
        IUserRepository userRepository,
        ILeadRepository leadRepository,
        IDealRepository dealRepository)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
    }

    public async Task<IReadOnlyList<SalesProductivityDto>> GetProductivityAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = (await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken)).Where(u => u.IsActive).ToList();
        var tasks = (await _taskRepository.GetByTenantAsync(tenantId, cancellationToken: cancellationToken)).ToList();
        var leads = (await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var deals = (await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();

        return users.Select(user =>
        {
            var userTasks = tasks.Where(t => t.AssignedToUserId == user.Id).ToList();
            var completed = userTasks.Where(t => t.Status == "Completed").ToList();
            var open = userTasks.Where(t => t.Status == "Open").ToList();

            var userLeads = leads.Where(l => l.AssignedToUserId == user.Id).ToList();
            double? avgResponse = null;
            var responseSamples = userLeads
                .Where(l => l.QualifiedAt.HasValue)
                .Select(l => (l.QualifiedAt!.Value - l.CreatedAt).TotalHours)
                .Where(h => h >= 0).ToList();
            if (responseSamples.Any())
                avgResponse = responseSamples.Average();

            var wonDeals = deals.Where(d => d.AssignedToUserId == user.Id && d.Stage == DealStage.ClosedWon && d.ClosedAt.HasValue).ToList();
            double? avgCycle = null;
            var cycleSamples = wonDeals.Select(d => (d.ClosedAt!.Value - d.CreatedAt).TotalDays).Where(d => d >= 0).ToList();
            if (cycleSamples.Any())
                avgCycle = cycleSamples.Average();

            return new SalesProductivityDto(
                user.Id,
                user.Email,
                completed.Count,
                open.Count(t => t.IsOverdue),
                open.Count,
                avgResponse.HasValue ? Math.Round(avgResponse.Value, 1) : null,
                avgCycle.HasValue ? Math.Round(avgCycle.Value, 1) : null,
                userTasks.Count);
        }).OrderByDescending(p => p.TasksCompleted).ToList();
    }
}

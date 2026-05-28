using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class AutonomousPlaybookEngine : IAutonomousPlaybookEngine
{
    private static readonly Dictionary<string, int> PlaybookSteps = new(StringComparer.OrdinalIgnoreCase)
    {
        [CustomerSuccessConstants.PlaybookOnboarding] = 3,
        [CustomerSuccessConstants.PlaybookAdoption] = 2,
        [CustomerSuccessConstants.PlaybookRescue] = 3,
        [CustomerSuccessConstants.PlaybookRenewal] = 3,
        [CustomerSuccessConstants.PlaybookExpansion] = 2,
        [CustomerSuccessConstants.PlaybookReEngagement] = 2
    };

    private readonly IAutonomousPlaybookStateRepository _stateRepository;
    private readonly ICustomerPlaybookService _playbooks;
    private readonly IUnitOfWork _unitOfWork;

    public AutonomousPlaybookEngine(
        IAutonomousPlaybookStateRepository stateRepository,
        ICustomerPlaybookService playbooks,
        IUnitOfWork unitOfWork)
    {
        _stateRepository = stateRepository;
        _playbooks = playbooks;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlaybookProgressDto> StartOrAdvanceAsync(
        Guid tenantId, Guid customerId, string playbookType, CancellationToken cancellationToken = default)
    {
        var steps = PlaybookSteps.GetValueOrDefault(playbookType, 2);
        var state = await _stateRepository.GetActiveAsync(tenantId, customerId, playbookType, cancellationToken);

        if (state == null)
        {
            state = AutonomousPlaybookState.Start(tenantId, customerId, playbookType, steps);
            await _stateRepository.AddAsync(state, cancellationToken);
            await _playbooks.ExecutePlaybookAsync(tenantId, customerId, playbookType, cancellationToken: cancellationToken);
        }
        else if (state.NextActionAt <= DateTime.UtcNow)
        {
            await _playbooks.ExecutePlaybookAsync(tenantId, customerId, playbookType, cancellationToken: cancellationToken);
            state.AdvanceStep($"PB_{playbookType}_step{state.CurrentStepIndex + 1}");
            await _stateRepository.UpdateAsync(state, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlaybookProgressDto(
            state.Id, customerId, playbookType, state.Status,
            state.CurrentStepIndex, state.TotalSteps, state.NextActionAt);
    }

    public async Task<int> ProcessDuePlaybooksAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var all = await _stateRepository.GetAllAsync(cancellationToken);
        var due = all.Where(s => s.TenantId == tenantId && s.Status == AutonomousConstants.PlaybookStateActive
                                 && s.NextActionAt <= DateTime.UtcNow).ToList();
        foreach (var s in due)
            await StartOrAdvanceAsync(tenantId, s.CustomerId, s.PlaybookType, cancellationToken);
        return due.Count;
    }
}

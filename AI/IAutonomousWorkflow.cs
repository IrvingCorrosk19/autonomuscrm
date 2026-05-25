namespace AutonomusCRM.AI;

/// <summary>
/// Workflows autónomos impulsados por IA — placeholder.
/// </summary>
public interface IAutonomousWorkflow
{
    Task<WorkflowRunResult> RunAsync(AutonomousWorkflowRequest request, CancellationToken cancellationToken = default);
}

public record AutonomousWorkflowRequest(
    string WorkflowId,
    string TenantId,
    string TriggerEvent,
    IReadOnlyDictionary<string, object>? Payload = null);

public record WorkflowRunResult(
    bool Completed,
    string Summary,
    IReadOnlyList<string> StepsExecuted,
    bool IsPlaceholder);

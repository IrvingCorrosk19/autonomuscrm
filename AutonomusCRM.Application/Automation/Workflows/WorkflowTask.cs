using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.Automation.Workflows;

/// <summary>
/// Tarea operativa creada por el motor de workflows.
/// </summary>
public class WorkflowTask : Entity
{
    public Guid TenantId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid WorkflowId { get; private set; }

    private WorkflowTask() : base()
    {
        Title = string.Empty;
        Status = "Open";
    }

    public static WorkflowTask Create(
        Guid tenantId,
        Guid workflowId,
        string title,
        string? description,
        Guid? relatedEntityId,
        string? relatedEntityType,
        Guid? assignedToUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title required", nameof(title));

        return new WorkflowTask
        {
            TenantId = tenantId,
            WorkflowId = workflowId,
            Title = title.Trim(),
            Description = description,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            AssignedToUserId = assignedToUserId,
            Status = "Open"
        };
    }

    public void Complete()
    {
        Status = "Completed";
        MarkAsUpdated();
    }
}

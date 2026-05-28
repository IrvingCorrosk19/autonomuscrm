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
    public DateTime? DueDate { get; private set; }
    public string Priority { get; private set; }
    public string? TaskType { get; private set; }

    private WorkflowTask() : base()
    {
        Title = string.Empty;
        Status = "Open";
        Priority = "Normal";
    }

    public static WorkflowTask Create(
        Guid tenantId,
        Guid workflowId,
        string title,
        string? description,
        Guid? relatedEntityId,
        string? relatedEntityType,
        Guid? assignedToUserId,
        DateTime? dueDate = null,
        string priority = "Normal",
        string? taskType = null)
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
            DueDate = dueDate,
            Priority = string.IsNullOrWhiteSpace(priority) ? "Normal" : priority,
            TaskType = taskType,
            Status = "Open"
        };
    }

    public bool IsOverdue => Status == "Open" && DueDate.HasValue && DueDate.Value < DateTime.UtcNow;

    public void Complete()
    {
        Status = "Completed";
        MarkAsUpdated();
    }

    public void AssignTo(Guid userId)
    {
        AssignedToUserId = userId;
        MarkAsUpdated();
    }

    public void SetDueDate(DateTime dueDate)
    {
        DueDate = dueDate;
        MarkAsUpdated();
    }

    public void SetPriority(string priority)
    {
        Priority = string.IsNullOrWhiteSpace(priority) ? "Normal" : priority;
        MarkAsUpdated();
    }
}

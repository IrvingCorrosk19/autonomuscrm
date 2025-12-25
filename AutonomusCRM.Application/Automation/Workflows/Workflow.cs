using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.Automation.Workflows;

/// <summary>
/// Entidad Workflow para Automation Engine
/// </summary>
public class Workflow : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public List<WorkflowTrigger> Triggers { get; private set; }
    public List<WorkflowCondition> Conditions { get; private set; }
    public List<WorkflowAction> Actions { get; private set; }
    public int ExecutionCount { get; private set; }
    public DateTime? LastExecutedAt { get; private set; }

    private Workflow() : base()
    {
        Name = string.Empty;
        Triggers = new List<WorkflowTrigger>();
        Conditions = new List<WorkflowCondition>();
        Actions = new List<WorkflowAction>();
        IsActive = true;
    }

    private Workflow(Guid id, Guid tenantId, string name) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Triggers = new List<WorkflowTrigger>();
        Conditions = new List<WorkflowCondition>();
        Actions = new List<WorkflowAction>();
        IsActive = true;
    }

    public static Workflow Create(Guid tenantId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del workflow no puede estar vacío", nameof(name));

        return new Workflow(Guid.NewGuid(), tenantId, name)
        {
            Description = description
        };
    }

    public void AddTrigger(WorkflowTrigger trigger)
    {
        Triggers.Add(trigger);
        MarkAsUpdated();
    }

    public void AddCondition(WorkflowCondition condition)
    {
        Conditions.Add(condition);
        MarkAsUpdated();
    }

    public void AddAction(WorkflowAction action)
    {
        Actions.Add(action);
        MarkAsUpdated();
    }

    public void RecordExecution()
    {
        ExecutionCount++;
        LastExecutedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void UpdateInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del workflow no puede estar vacío", nameof(name));

        Name = name;
        Description = description;
        MarkAsUpdated();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        MarkAsUpdated();
    }

    public void ClearTriggers()
    {
        Triggers.Clear();
        MarkAsUpdated();
    }

    public void ClearConditions()
    {
        Conditions.Clear();
        MarkAsUpdated();
    }

    public void ClearActions()
    {
        Actions.Clear();
        MarkAsUpdated();
    }
}

public class WorkflowTrigger
{
    public string Type { get; set; } = string.Empty; // "DomainEvent", "StateChange", "Webhook", etc.
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class WorkflowCondition
{
    public string Type { get; set; } = string.Empty; // "BusinessRule", "Threshold", "Prediction", etc.
    public string Expression { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class WorkflowAction
{
    public string Type { get; set; } = string.Empty; // "Assign", "Communicate", "UpdateStatus", "CreateTask", "ActivateAgent"
    public string Target { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}


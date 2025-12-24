using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.Policies;

/// <summary>
/// Entidad Policy para Policy Engine
/// </summary>
public class Policy : Entity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Expression { get; private set; }
    public bool IsActive { get; private set; }

    private Policy() : base()
    {
        Name = string.Empty;
        Expression = string.Empty;
        IsActive = true;
    }

    private Policy(Guid id, Guid tenantId, string name, string expression) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Expression = expression;
        IsActive = true;
    }

    public static Policy Create(Guid tenantId, string name, string expression, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la política no puede estar vacío", nameof(name));

        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("La expresión de la política no puede estar vacía", nameof(expression));

        return new Policy(Guid.NewGuid(), tenantId, name, expression)
        {
            Description = description
        };
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void UpdateExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("La expresión no puede estar vacía", nameof(expression));

        Expression = expression;
        MarkAsUpdated();
    }
}


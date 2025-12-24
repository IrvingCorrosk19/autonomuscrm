namespace AutonomusCRM.Domain.ValueObjects;

/// <summary>
/// Value Object para identificador de tenant (multi-tenant)
/// </summary>
public sealed class TenantId : ValueObject
{
    public Guid Value { get; }

    private TenantId(Guid value)
    {
        Value = value;
    }

    public static TenantId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId no puede ser vacío", nameof(value));

        return new TenantId(value);
    }

    public static TenantId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator Guid(TenantId tenantId) => tenantId.Value;
    public static implicit operator TenantId(Guid value) => Create(value);
}


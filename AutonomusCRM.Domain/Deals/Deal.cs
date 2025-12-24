using AutonomusCRM.Domain.Common;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Deals.Events;

namespace AutonomusCRM.Domain.Deals;

/// <summary>
/// Entidad Deal - Oportunidad de negocio
/// </summary>
public class Deal : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? ExpectedAmount { get; private set; }
    public DealStatus Status { get; private set; }
    public DealStage Stage { get; private set; }
    public int? Probability { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? ExpectedCloseDate { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private Deal() : base()
    {
        Title = string.Empty;
        Status = DealStatus.Open;
        Stage = DealStage.Prospecting;
        Metadata = new Dictionary<string, object>();
    }

    private Deal(Guid id, Guid tenantId, Guid customerId, string title, decimal amount) : base(id)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        Title = title;
        Amount = amount;
        Status = DealStatus.Open;
        Stage = DealStage.Prospecting;
        Metadata = new Dictionary<string, object>();
    }

    public static Deal Create(Guid tenantId, Guid customerId, string title, decimal amount, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título del deal no puede estar vacío", nameof(title));

        if (amount <= 0)
            throw new ArgumentException("El monto del deal debe ser mayor a cero", nameof(amount));

        var deal = new Deal(Guid.NewGuid(), tenantId, customerId, title, amount)
        {
            Description = description
        };

        deal.AddDomainEvent(new DealCreatedEvent(deal.Id, tenantId, customerId, title, amount));
        return deal;
    }

    public void UpdateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto del deal debe ser mayor a cero", nameof(amount));

        Amount = amount;
        MarkAsUpdated();
        AddDomainEvent(new DealAmountUpdatedEvent(Id, TenantId, amount));
    }

    public void UpdateStage(DealStage stage, int? probability = null)
    {
        if (Stage == stage)
            return;

        var oldStage = Stage;
        Stage = stage;
        Probability = probability ?? CalculateProbabilityForStage(stage);
        MarkAsUpdated();
        AddDomainEvent(new DealStageChangedEvent(Id, TenantId, oldStage, stage, Probability.Value));
    }

    public void AssignToUser(Guid userId)
    {
        AssignedToUserId = userId;
        MarkAsUpdated();
        AddDomainEvent(new DealAssignedEvent(Id, TenantId, userId));
    }

    public void SetExpectedCloseDate(DateTime date)
    {
        ExpectedCloseDate = date;
        MarkAsUpdated();
    }

    public void Close(DateTime closedAt, decimal? finalAmount = null)
    {
        if (Status == DealStatus.Closed)
            return;

        Status = DealStatus.Closed;
        Stage = DealStage.ClosedWon;
        ClosedAt = closedAt;
        if (finalAmount.HasValue)
            Amount = finalAmount.Value;

        MarkAsUpdated();
        AddDomainEvent(new DealClosedEvent(Id, TenantId, closedAt, Amount));
    }

    public void Lose(string? reason = null)
    {
        if (Status == DealStatus.Closed)
            return;

        Status = DealStatus.Closed;
        Stage = DealStage.ClosedLost;
        ClosedAt = DateTime.UtcNow;
        MarkAsUpdated();

        if (!string.IsNullOrWhiteSpace(reason))
            Metadata["LossReason"] = reason;

        AddDomainEvent(new DealLostEvent(Id, TenantId, reason));
    }

    public void UpdateMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("La clave de metadata no puede estar vacía", nameof(key));

        Metadata[key] = value;
        MarkAsUpdated();
    }

    private static int CalculateProbabilityForStage(DealStage stage) => stage switch
    {
        DealStage.Prospecting => 10,
        DealStage.Qualification => 25,
        DealStage.Proposal => 50,
        DealStage.Negotiation => 75,
        DealStage.ClosedWon => 100,
        DealStage.ClosedLost => 0,
        _ => 0
    };
}

public enum DealStatus
{
    Open = 0,
    Closed = 1,
    OnHold = 2,
    Cancelled = 3
}

public enum DealStage
{
    Prospecting = 0,
    Qualification = 1,
    Proposal = 2,
    Negotiation = 3,
    ClosedWon = 4,
    ClosedLost = 5
}

